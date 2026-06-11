---
title: "웹훅 데이터 모델 및 비동기 발송 인프라 구축"
phase: 21
wave: 1
depends_on: []
files_modified:
  - Aristokeides.Api/Models/Webhook.cs
  - Aristokeides.Api/Models/WebhookDelivery.cs
  - Aristokeides.Api/Data/AppDbContext.cs
  - Aristokeides.Api/Services/Webhook/WebhookQueue.cs
  - Aristokeides.Api/Services/Webhook/WebhookBackgroundWorker.cs
autonomous: true
requirements:
  - "Webhook 및 WebhookDelivery 데이터 모델을 구축하고 3개 데이터베이스 공급자 마이그레이션에 적용한다."
  - "System.Threading.Channels 기반의 WebhookQueue 및 백그라운드에서 발송 작업을 비동기로 처리할 WebhookBackgroundWorker의 뼈대를 구현한다."
---

# Plan 21A: 웹훅 데이터 모델 및 비동기 발송 인프라 구축

## Objective

저장소 이벤트의 비동기 전송을 지원하기 위해 웹훅 및 웹훅 발송 이력(Delivery Log)의 데이터베이스 모델을 생성하고 적용합니다. 또한, 발송 작업이 동기식 웹 요청 흐름을 방해하지 않도록 메모리 내 고성능 채널 큐(`System.Threading.Channels`)와 백그라운드 호스트 서비스(`WebhookBackgroundWorker`)로 구성된 인프라를 확립합니다.

## Tasks

<task id="21A-1">
<title>웹훅 데이터 모델 정의 및 DB 매핑</title>
<read_first>
- `Aristokeides.Api/Data/AppDbContext.cs` — 기존 복합 키 및 모델 매핑 양식 참조
- `Aristokeides.Api/Models/Repository.cs` — 기존 저장소 정의 참조
</read_first>
<action>
1. `Aristokeides.Api/Models/Webhook.cs` 모델 정의:
   ```csharp
   using System;
   using System.Collections.Generic;

   namespace Aristokeides.Api.Models;

   public class Webhook
   {
       public int Id { get; set; }
       public Guid RepositoryId { get; set; }
       public required string Url { get; set; }
       public string? Secret { get; set; }
       public required string ContentType { get; set; } = "application/json"; // "application/json"
       public required string WebhookType { get; set; } = "Generic"; // "Generic", "Slack", "Discord"
       public bool IsActive { get; set; } = true;
       public required string TriggerEvents { get; set; } = "push"; // Comma-separated: "push,issue,pull_request"
       public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

       public Repository Repository { get; set; } = null!;
       public ICollection<WebhookDelivery> Deliveries { get; set; } = new List<WebhookDelivery>();
   }
   ```
2. `Aristokeides.Api/Models/WebhookDelivery.cs` 모델 정의:
   ```csharp
   using System;

   namespace Aristokeides.Api.Models;

   public class WebhookDelivery
   {
       public int Id { get; set; }
       public int WebhookId { get; set; }
       public Guid DeliveryId { get; set; } // UUID
       public required string EventType { get; set; } // "push", "issue", "pull_request"
       public string? RequestHeaders { get; set; } // JSON
       public string? RequestBody { get; set; }
       public string? ResponseHeaders { get; set; }
       public string? ResponseBody { get; set; }
       public int HttpStatusCode { get; set; }
       public long DurationMs { get; set; }
       public bool IsSuccess { get; set; }
       public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;

       public Webhook Webhook { get; set; } = null!;
   }
   ```
3. `Aristokeides.Api/Data/AppDbContext.cs` 수정 및 제약 추가:
   - `DbSet<Webhook>` 및 `DbSet<WebhookDelivery>` 선언 추가.
   - `OnModelCreating` 에 웹훅 엔티티 매핑 지정:
     - Webhook: `Url` 최대 1024자, `Secret` 최대 256자 지정.
     - WebhookDelivery: `EventType` 최대 50자 지정.
     - 외래키 삭제 규칙 지정.
</action>
<acceptance_criteria>
- 신규 모델 선언 및 AppDbContext 매핑 추가 후 프로젝트가 컴파일 오류 없이 정상 빌드된다.
</acceptance_criteria>
</task>

<task id="20A-2">
<title>데이터베이스 마이그레이션 생성 및 반영</title>
<read_first>
- `Aristokeides.Api/Migrations/` — 기존 데이터베이스 프로바이더 마이그레이션 폴더 확인
</read_first>
<action>
1. SQLite, PostgreSQL, MySQL 세 가지 공급자용 EF Core 마이그레이션을 생성하고, 로컬 SQLite DB에 업데이트를 반영한다.
   - `dotnet ef migrations add AddWebhookModels --context SqliteAppDbContext -o Migrations/Sqlite`
   - `dotnet ef migrations add AddWebhookModels --context PostgresAppDbContext -o Migrations/Postgres`
   - `dotnet ef migrations add AddWebhookModels --context MysqlAppDbContext -o Migrations/Mysql`
   - `dotnet ef database update --context SqliteAppDbContext`
</action>
<acceptance_criteria>
- 세 개 DB 프로바이더의 마이그레이션 파일이 컴파일/충돌 에러 없이 정상 생성된다.
- SQLite 개발 DB에 `Webhooks` 및 `WebhookDeliveries` 테이블이 정상 생성되고 제약사항이 반영된다.
</acceptance_criteria>
</task>

<task id="21A-3">
<title>비동기 Webhook Queue & Background Worker 기초 구현</title>
<read_first>
- `Aristokeides.Api/Services/RepositoryCreationBackgroundWorker.cs` — 기존 Channel 기반 비동기 워커 구현 참조
</read_first>
<action>
1. `Aristokeides.Api/Services/Webhook/WebhookQueue.cs` 구현:
   - `System.Threading.Channels.Channel<WebhookTask>`를 멤버로 가진 싱글톤 큐 서비스 작성.
   - 큐에 작업을 비동기로 인큐(`QueueWebhookTaskAsync`) 및 디큐(`DequeueWebhookTaskAsync`)하는 스레드 세이프 메서드 노출.
   - DTO `WebhookTask` 정의:
     ```csharp
     public class WebhookTask
     {
         public int WebhookId { get; set; }
         public Guid DeliveryId { get; set; }
         public string TargetUrl { get; set; } = "";
         public string? Secret { get; set; }
         public string WebhookType { get; set; } = "Generic";
         public string ContentType { get; set; } = "application/json";
         public string EventType { get; set; } = "";
         public string Payload { get; set; } = "";
     }
     ```
2. `Aristokeides.Api/Services/Webhook/WebhookBackgroundWorker.cs` 구현:
   - `BackgroundService`를 상속받아 `ExecuteAsync`에서 `WebhookQueue`를 반복 관찰(Dequeue)하면서 작업을 꺼냅니다.
   - 꺼낸 작업은 HttpClient를 주입받아 비동기 HTTP POST로 발송하는 뼈대 로직을 작성합니다. (실제 HTTP 요청 세부 처리 및 이력 기록은 21B단계에서 고도화)
3. `Program.cs`에 `WebhookQueue`를 Singleton으로, `WebhookBackgroundWorker`를 HostedService로 등록합니다.
</action>
<acceptance_criteria>
- `WebhookQueue`에 작업을 집어넣고 `WebhookBackgroundWorker`가 백그라운드 태스크로서 에러 없이 감지/디큐해가는 연동 뼈대가 검증된다.
- 프로젝트 빌드가 성공한다.
</acceptance_criteria>
</task>

## must_haves

- 웹훅 전송용 백그라운드 서비스 및 작업 큐는 반드시 비동기 스레드에서 돌아야 하며, 메인 스레드를 블록(Sync wait)하지 않아야 한다.
- SQLite 데이터베이스 스키마 상에 `Webhooks` 및 `WebhookDeliveries` 테이블과 적절한 외래키 연동 및 컬럼 크기 제약조건이 마련되어야 한다.

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Models/Webhook.cs` | 신규 | 웹훅 설정 메타데이터 데이터 모델 |
| `Aristokeides.Api/Models/WebhookDelivery.cs` | 신규 | 웹훅 발송 상세 이력 정보 데이터 모델 |
| `Aristokeides.Api/Data/AppDbContext.cs` | 수정 | DbSet 추가 및 온모델크리에이팅 매핑 정의 |
| `Aristokeides.Api/Services/Webhook/WebhookQueue.cs` | 신규 | Channel 기반 비동기 메모리 큐 서비스 |
| `Aristokeides.Api/Services/Webhook/WebhookBackgroundWorker.cs` | 신규 | 웹훅 발송 백그라운드 Hosted Service 워커 |
