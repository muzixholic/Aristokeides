---
title: "Git LFS 기본 데이터 모델 구축 및 API 컨트롤러 뼈대 구현"
phase: 20
wave: 1
depends_on: []
files_modified:
  - Aristokeides.Api/Models/LfsLock.cs
  - Aristokeides.Api/Models/LfsObject.cs
  - Aristokeides.Api/Data/AppDbContext.cs
  - Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs
  - Aristokeides.Api/Controllers/LfsApiController.cs
autonomous: true
requirements:
  - "LfsLock 및 LfsObject 엔티티를 정의하고 데이터베이스 테이블 매핑 및 고유성 제약 조건을 생성한다."
  - "GitSmartHttpMiddleware에서 LFS 관련 요청 경로(/{owner}/{repo}.git/info/lfs/...)를 감지하여 바로 파이프라인 다음 단계로 제어를 넘기도록 우회 처리한다."
  - "LfsApiController에서 Basic Auth 인증을 거치는 LFS Batch API 및 Locks API 규격의 스켈레톤 액션을 개발하고, 락 상태 조회 및 락 해제에 대한 구조를 마련한다."
---

# Plan 20A: Git LFS 기본 데이터 모델 구축 및 API 컨트롤러 뼈대 구현

## Objective

Git LFS 지원을 위한 첫 단계로 데이터 모델(`LfsLock`, `LfsObject`)을 정의하고 마이그레이션을 적용합니다. 또한 Git HTTP 스마트 미들웨어에서 LFS 요청이 걸려 `git http-backend`로 넘어가는 것을 방지하고, LFS 컨트롤러에서 Basic Auth 인증을 처리할 수 있도록 라우팅 및 기본 뼈대 액션들을 설계합니다.

## Tasks

<task id="20A-1">
<title>LFS 데이터 모델 정의 및 DB Context 매핑</title>
<read_first>
- `Aristokeides.Api/Models/User.cs` — 기존 사용자 엔터티 참조
- `Aristokeides.Api/Models/Repository.cs` — 기존 저장소 엔터티 참조
- `Aristokeides.Api/Data/AppDbContext.cs` — DB Context 구성 방식 참조
</read_first>
<action>
1. `Aristokeides.Api/Models/LfsLock.cs` 모델 생성:
   - 특정 저장소(`RepositoryId`)의 특정 경로(`Path`)에 대해 특정 사용자(`UserId`)가 잠금(Lock)을 생성한 정보를 보관합니다.
   ```csharp
   using System;

   namespace Aristokeides.Api.Models;

   public class LfsLock
   {
       public int Id { get; set; }
       public Guid RepositoryId { get; set; }
       public int UserId { get; set; }
       public required string Path { get; set; }
       public DateTime LockedAt { get; set; } = DateTime.UtcNow;

       public Repository Repository { get; set; } = null!;
       public User User { get; set; } = null!;
   }
   ```
2. `Aristokeides.Api/Models/LfsObject.cs` 모델 생성:
   - 업로드된 LFS 개체(바이너리)의 메타데이터(OID, 크기 등)를 저장소별로 관리하기 위해 모델링합니다.
   ```csharp
   using System;

   namespace Aristokeides.Api.Models;

   public class LfsObject
   {
       public int Id { get; set; }
       public Guid RepositoryId { get; set; }
       public required string Oid { get; set; } // SHA-256 (64자)
       public long Size { get; set; }
       public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

       public Repository Repository { get; set; } = null!;
   }
   ```
3. `Aristokeides.Api/Data/AppDbContext.cs` 수정 및 매핑 적용:
   - `DbSet<LfsLock>` 및 `DbSet<LfsObject>` 추가.
   - `OnModelCreating` 에 제약 조건 추가:
     - `LfsLock`: `(RepositoryId, Path)` 복합 유니크 인덱스 생성, `Path` 최대 길이 512자 지정.
     - `LfsObject`: `(RepositoryId, Oid)` 복합 유니크 인덱스 생성, `Oid` 최대 길이 64자 지정.
     - 외래키 삭제 제약(`DeleteBehavior.Cascade`) 설정.
</action>
<acceptance_criteria>
- 빌드 에러 없이 프로젝트 컴파일이 성공적으로 수행된다.
- `LfsLock` 및 `LfsObject` 클래스가 정상 정의되고 DB 컨텍스트에 포함된다.
</acceptance_criteria>
</task>

<task id="20A-2">
<title>데이터베이스 마이그레이션 생성 및 적용</title>
<read_first>
- `Aristokeides.Api/Migrations/` — 기존 마이그레이션 파일 및 3개 DB 프로바이더 구성 참조
</read_first>
<action>
1. SQLite, PostgreSQL, MySQL 각각의 데이터베이스 공급자용 EF Core 마이그레이션을 생성하고 적용한다.
   - `dotnet ef migrations add AddLfsModels --context SqliteAppDbContext -o Migrations/Sqlite`
   - `dotnet ef migrations add AddLfsModels --context PostgresAppDbContext -o Migrations/Postgres`
   - `dotnet ef migrations add AddLfsModels --context MysqlAppDbContext -o Migrations/Mysql`
2. 로컬 SQLite 개발 데이터베이스를 업데이트한다:
   - `dotnet ef database update --context SqliteAppDbContext`
</action>
<acceptance_criteria>
- 에러 없이 세 개 프로바이더용 마이그레이션이 정상 생성된다.
- SQLite 개발 DB에 신규 테이블(`LfsLocks`, `LfsObjects`)이 생성되고 제약 조건이 정상적으로 적용된다.
</acceptance_criteria>
</task>

<task id="20A-3">
<title>Git HTTP 미들웨어 LFS 바이패스 적용</title>
<read_first>
- `Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs` — 미들웨어 라우팅 판단 로직 참조
</read_first>
<action>
1. `GitSmartHttpMiddleware.cs`의 `InvokeAsync` 도입부에 LFS 관련 엔드포인트 바이패스 조건 추가.
   - Git HTTP 요청 패턴 분할 시, 세그먼트의 구조가 `/{owner}/{repo}.git/info/lfs/...` 와 같은 경우 미들웨어 인증 및 `git http-backend` 처리를 타지 않도록 감지하여 `_next(context)`를 호출하고 바로 함수를 리턴하게 한다.
   ```csharp
   // LFS API 요청 우회 (/{username}/{repo.name}.git/info/lfs/...)
   if (segments.Length >= 4 && 
       segments[2].Equals("info", StringComparison.OrdinalIgnoreCase) && 
       segments[3].Equals("lfs", StringComparison.OrdinalIgnoreCase))
   {
       await _next(context);
       return;
   }
   ```
</action>
<acceptance_criteria>
- GitSmartHttpMiddleware 수정 후 기존 Git Smart HTTP 기능(Pull/Push/Clone)이 영향 없이 정상 작동하는지 확인.
- LFS API 엔드포인트(`/info/lfs/objects/batch` 등) 호출 시 404가 아닌 컨트롤러 매핑으로 안전하게 라우팅 경로가 이어지는지 검증.
</acceptance_criteria>
</task>

<task id="20A-4">
<title>LfsApiController 기본 스켈레톤 구현</title>
<read_first>
- `Aristokeides.Api/Controllers/` — 기존 컨트롤러 구조 및 Basic Auth 속성 적용 방식 참조
</read_first>
<action>
1. `Aristokeides.Api/Controllers/LfsApiController.cs` 컨트롤러 생성:
   - 라우팅 경로: `[Route("{owner}/{repo}.git/info/lfs")]`
   - Basic Auth 정책 적용: `[Authorize(AuthenticationSchemes = "Basic")]`
   - 엔드포인트 구현:
     - `POST /objects/batch`: LFS Batch API. 클라이언트의 요청 본문 구조를 역직렬화하는 Request DTO 정의 및 응답 스키마 기본 구성.
     - `POST /locks`: 락 생성 API.
     - `GET /locks`: 락 목록 조회 API.
     - `POST /locks/verify`: 락 검증 API.
     - `POST /locks/{id}/unlock`: 락 해제 API.
   - 요청 시 `owner` 및 `repo`가 데이터베이스에 존재하는지 확인하고, 해당 사용자가 저장소에 접근 가능한 권한이 있는지 기존 검증 패턴을 적용한다.
</action>
<acceptance_criteria>
- `/info/lfs/objects/batch` 및 `/locks` API 호출 시 Basic Auth 인증 처리가 정상 수행되며 401/403/200 등의 응답 분기가 수행된다.
- C# 빌드가 정상적으로 완료된다.
</acceptance_criteria>
</task>

## must_haves

- LFS API 호출 시 Basic Auth 인증 및 저장소 소유권/팀 권한에 근거한 접근 제어가 올바르게 수행되어야 한다.
- 데이터 모델의 `(RepositoryId, Path)` 복합 제약조건을 통해 파일 락의 중복 생성을 원천 방지해야 한다.
- Git 스마트 미들웨어가 LFS 경로에 영향받아 백엔드 `git` 프로세스를 오작동으로 호출하지 않아야 한다.

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Models/LfsLock.cs` | 신규 | LFS 잠금 정보 모델 |
| `Aristokeides.Api/Models/LfsObject.cs` | 신규 | LFS 업로드 메타데이터 정보 모델 |
| `Aristokeides.Api/Data/AppDbContext.cs` | 수정 | LFS 테이블 DbSet 선언 및 유니크 제약 설정 |
| `Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs` | 수정 | LFS 관련 경로의 미들웨어 바이패스 조건 추가 |
| `Aristokeides.Api/Controllers/LfsApiController.cs` | 신규 | LFS Batch 및 Locks API 컨트롤러 스켈레톤 |
