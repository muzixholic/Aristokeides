---
title: "웹훅 CRUD API 및 비동기 발송 로직 완성"
phase: 21
wave: 2
depends_on: ["21A"]
files_modified:
  - Aristokeides.Api/Services/Webhook/WebhookService.cs
  - Aristokeides.Api/Services/Webhook/WebhookBackgroundWorker.cs
  - Aristokeides.Api/Controllers/WebhookApiController.cs
autonomous: true
requirements:
  - "WebhookService를 통해 웹훅 관리 CRUD, SSRF 방지 URL 유효성 검사 및 HMAC-SHA256 서명 계산, 수동 재전송(Redelivery) 로직을 구현한다."
  - "WebhookBackgroundWorker가 실제 HTTP POST 발송 시 요청/응답 페이로드를 가공해 WebhookDelivery 이력 테이블에 저장하게 하고 64KB 절삭 및 10초 타임아웃을 적용한다."
  - "WebhookApiController를 신규 생성하여 저장소별 웹훅 설정 CRUD 및 전송 로그 조회, 수동 재전송 트리거 API 엔드포인트를 제공한다."
---

# Plan 21B: 웹훅 CRUD API 및 비동기 발송 로직 완성

## Objective

웹훅을 등록, 수정, 삭제하는 API 컨트롤러를 구현하고, 관리자가 이력 화면에서 실패한 웹훅을 손쉽게 다시 인큐(큐에 재적재)할 수 있는 재전송(Redelivery) 핵심 로직을 탑재합니다. 발송 컨텍스트(`WebhookBackgroundWorker`)에서는 무결성 검증용 HMAC-SHA256 헤더 서명을 생성하고, 타임아웃과 수신 응답 절삭(SSRF 및 DoS 차단) 처리를 추가하여 발송 비즈니스 안전성을 높입니다.

## Tasks

<task id="21B-1">
<title>WebhookService 비즈니스 서비스 구현</title>
<read_first>
- `Aristokeides.Api/Services/Webhook/WebhookQueue.cs` — 앞서 구현한 큐 서비스 참조
- `Aristokeides.Api/Models/Webhook.cs` — 웹훅 설정 모델 참조
</read_first>
<action>
1. `Aristokeides.Api/Services/Webhook/WebhookService.cs` 신규 구현:
   - **유효성 검사 (SSRF 방어):** 웹훅 추가/수정 시 URL이 사설 IP 주소 대역이나 로컬 루프백(`localhost`, `127.0.0.1` 등)을 향하는지 판별하고, 개발 환경이 아닌 상용 운영 모드일 경우 에러(ArgumentException)를 던집니다.
   - **서명 해시 계산:** `HMACSHA256`을 사용하여 문자열 페이로드에 대해 `sha256={hash}` 형식의 서명 문자열을 생성하는 유틸리티 메서드 작성.
   - **이벤트 트리거 디스패치:** 특정 저장소에 구독된 활성 웹훅(`IsActive == true`) 목록을 가져와, 각 웹훅 설정에 트리거 이벤트(예: `push`)가 매핑되어 있는 경우 전송 고유 UUID를 발급하고 `WebhookDelivery` 로그 레코드를 선작성한 후 `WebhookQueue`에 작업을 밀어넣는 헬퍼 메서드 (`TriggerWebhookAsync`) 구현.
   - **수동 재전송 (Redelivery):** 특정 `WebhookDelivery` 레코드 ID를 입력받아 기존 페이로드와 설정을 읽어들인 뒤, 새로운 UUID와 `WebhookDelivery` 신규 로그를 기록하고 동일 바디 페이로드로 `WebhookQueue`에 재발송 요소를 추가하는 메서드 (`RedeliverAsync`) 구현.
2. `Program.cs`에 `WebhookService`를 Scoped 서비스로 등록합니다.
</action>
<acceptance_criteria>
- 사설 IP URL 검사(SSRF 필터)가 단위 테스트를 통해 정상적으로 반려 처리된다.
- 재전송 트리거 시 새로운 Delivery UUID를 부여하여 WebhookQueue에 작업이 안전하게 재생성 인큐된다.
</acceptance_criteria>
</task>

<task id="21B-2">
<title>WebhookBackgroundWorker 발송 및 이력 로깅 세부 구현</title>
<read_first>
- `Aristokeides.Api/Services/Webhook/WebhookBackgroundWorker.cs` — 21A에서 생성한 백그라운드 뼈대 코드 참조
</read_first>
<action>
1. `WebhookBackgroundWorker.cs`에 `IHttpClientFactory` 및 `IServiceProvider` 주입.
2. Dequeue되어 처리되는 개별 `WebhookTask`에 대해 다음의 세부 발송 및 로깅 로직 구현:
   - **서명 적용:** 웹훅의 `Secret` 키가 기입되어 있는 경우, `WebhookService` 서명 메서드를 통해 서명을 계산하여 헤더 `X-Aristokeides-Signature-256`에 주입.
   - **HTTP Client 구성:** 타임아웃을 10초로 제한하여 클라이언트 연결 구성.
   - **요청 전송:** POST body에 페이로드 적재 및 `application/json` 컨텐트 타입 적용. 공통 헤더 (`X-Aristokeides-Event`, `X-Aristokeides-Delivery` UUID) 주입.
   - **소요 시간 측정:** `Stopwatch`를 사용해 발송 소요 시간 측정.
   - **응답 수신 및 절삭 (DoS 차단):** 수신한 HTTP 응답 본문을 최대 64KB(65,536바이트)까지만 버퍼로 읽고, 초과 분량은 절삭하여 로그에 기록.
   - **결과 데이터베이스 영속화:** 발송 중 네트워크 예외가 발생하더라도 try-catch로 감싸 `WebhookDelivery` 테이블에 상태 코드 `0` 및 에러 메시지를 응답 본문 로그로 안전하게 비동기 업데이트 처리.
</action>
<acceptance_criteria>
- 응답 수신 시 비정상적으로 큰 본문은 64KB 근처에서 잘려 DB에 무리 없이 기록된다.
- 타겟 서버가 다운되었거나 잘못된 URL인 경우 DB 전송 이력이 실패 상태(HTTP 0)로 정상 업데이트된다.
</acceptance_criteria>
</task>

<task id="21B-3">
<title>WebhookApiController 구현</title>
<read_first>
- `Aristokeides.Api/Controllers/LfsApiController.cs` — 기존 저장소 권한 및 Basic Auth 컨트롤러 연동 코드 참조
</read_first>
<action>
1. `Aristokeides.Api/Controllers/WebhookApiController.cs` 신규 구현:
   - 라우팅: `[Route("api/repos/{owner}/{repo}/webhooks")]`
   - 인증: `[Authorize(AuthenticationSchemes = "Basic")]`
   - 엔드포인트 구현:
     - `GET /`: 특정 저장소에 등록된 웹훅 목록 조회. (단, API 응답 시 Secret 정보는 `********`로 마스킹 처리하여 절대 노출하지 않는다)
     - `POST /`: 웹훅 추가. (URL, Type, TriggerEvents, Secret, IsActive 등 바인딩)
     - `GET /{id}`: 개별 웹훅 설정 조회.
     - `PUT /{id}`: 웹훅 설정 수정.
     - `DELETE /{id}`: 웹훅 제거.
     - `GET /{id}/deliveries`: 특정 웹훅의 전송 이력 목록 조회 (최근 50개 등 역순 정렬).
     - `GET /{id}/deliveries/{deliveryId}`: 전송 이력 상세 단건 조회 (요청/응답 헤더 및 본문 상세 로그 포함).
     - `POST /deliveries/{deliveryId}/redeliver`: 특정 이력에 대한 수동 재전송(Redelivery) 지시. (`WebhookService.RedeliverAsync` 호출 후 결과 반환)
</action>
<acceptance_criteria>
- 웹훅 정보 조회 시 `Secret` 설정이 되어 있다면 응답 JSON에서 평문 비밀값이 노출되지 않고 마스킹 처리된다.
- `/redeliver` API 호출 완료 시 200 OK와 함께 재전송 작업이 큐에 즉시 적재된다.
</acceptance_criteria>
</task>

## must_haves

- API 응답 시 웹훅의 `Secret` 키는 반드시 마스킹 처리되어 노출을 차단해야 한다.
- 웹훅 발송 컨텍스트(`BackgroundWorker`)는 대상 서버의 네트워크 오류 및 타임아웃 발생 시에도 크래시 없이 DB에 실패 이력을 기록해야 한다.
- 수동 재전송 API(`/redeliver`) 호출은 해당 저장소에 쓰기(`Write`) 이상의 권한을 가진 사용자에게만 인가되어야 한다.

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Services/Webhook/WebhookService.cs` | 신규 | 웹훅 CRUD 관리 및 서명, 발송 디스패치 비즈니스 서비스 |
| `Aristokeides.Api/Controllers/WebhookApiController.cs` | 신규 | 저장소 웹훅 관리 및 이력 로그, 재전송 제어 API 컨트롤러 |
