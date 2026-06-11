# Phase 21: 웹훅 및 외부 연동 - Research

**Date:** 2026-06-10
**Status:** Completed

## 1. Webhook Core Architecture & Event Payloads

웹훅 시스템은 저장소에서 발생하는 이벤트를 가입된 외부 엔드포인트 URL로 배달하는 푸시(Push) 메커니즘입니다.

### 1.1. 이벤트 유형 및 공통 페이로드 규격
모든 웹훅 요청에는 아래 헤더가 필수 포함됩니다:
- `X-Aristokeides-Event`: 이벤트 유형 (`push`, `issue`, `pull_request`)
- `X-Aristokeides-Delivery`: 전송 고유 ID (UUID)
- `X-Aristokeides-Signature-256`: HMAC-SHA256 서명값 (Secret 설정 시)

공통 페이로드 기본 본문 구조:
```json
{
  "event": "push",
  "repository": {
    "id": "guid...",
    "name": "repo-name",
    "owner": "owner-name"
  },
  "sender": {
    "id": 1,
    "username": "user-a"
  },
  "data": { ... }
}
```

---

## 2. Background Dispatching (Channel & Worker)

동기식 HTTP 호출은 성능 저하 및 수신 서버 장애 시 응답 대기를 초래합니다. `System.Threading.Channels`를 사용하여 프로듀서-컨슈머 모델을 정립합니다.

- **Channel 선언:** `Channel<WebhookTask>`
- **WebhookTask DTO:**
  - `WebhookId`: int
  - `Payload`: string (JSON)
  - `Headers`: Dictionary<string, string>
  - `TargetUrl`: string
  - `Secret`: string?
  - `WebhookDeliveryId`: int (로그 추적용)
- **Background Worker:**
  - `WebhookBackgroundWorker : BackgroundService`
  - 채널의 메시지를 지속 수신하여 `HttpClient`를 주입받아 POST 요청을 보냅니다.
  - 전송 완료 후 결과(HTTP Status, Request/Response body, Duration)를 `WebhookDelivery` 테이블에 비동기로 업데이트합니다.

---

## 3. Webhook Security Signature (HMAC-SHA256)

수신 측이 요청의 무결성을 검증하기 위해 HMAC-SHA256 해시를 헤더에 포함합니다.

```csharp
using System.Security.Cryptography;
using System.Text;

public static string GenerateSignature(string payload, string secret)
{
    var keyBytes = Encoding.UTF8.GetBytes(secret);
    var payloadBytes = Encoding.UTF8.GetBytes(payload);
    using var hmac = new HMACSHA256(keyBytes);
    var hashBytes = hmac.ComputeHash(payloadBytes);
    return "sha256=" + Convert.ToHexString(hashBytes).ToLowerInvariant();
}
```

---

## 4. Slack & Discord Message Payload Mapping

사용자가 Slack/Discord Incoming Webhook URL을 등록할 경우, 범용 JSON 페이로드를 읽기 쉬운 메신저 메시지로 변환합니다.

### 4.1. Slack Payload 어댑터 규칙
- Slack Incoming Webhook은 `{ "text": "..." }` 또는 `attachments` 포맷을 요구합니다.
- 예 (Issue 오픈 이벤트 변환):
  ```json
  {
    "text": "✏️ *[repo-name]* Issue <https://localhost:5001/user/repo/issues/1|#1> opened by *user-a*\n> *Title:* Issue Title\n> Description here..."
  }
  ```

### 4.2. Discord Payload 어댑터 규칙
- Discord Incoming Webhook은 `{ "content": "..." }` 또는 `embeds` 포맷을 요구합니다.
- 예 (Push 이벤트 변환):
  ```json
  {
    "content": "🚀 **[repo-name:main]** 2 new commits pushed by **user-a**:\n- `a1b2c3d` Commit message 1\n- `e5f6g7h` Commit message 2"
  }
  ```

---

## 5. UI Layout & Redelivery Logic

### 5.1. 웹훅 관리 화면 (CRUD)
- 저장소 설정 영역에 웹훅 탭 개설.
- 웹훅 추가 양식: Target URL, Webhook Type (`Generic`, `Slack`, `Discord`), Secret, 활성화 여부(IsActive), 트리거할 이벤트 선택.
- 전송 기록 목록: 최근 50개의 전송 내역(성공/실패 여부 아이콘, HTTP 코드, 전송 시간)을 타임라인으로 노출.

### 5.2. 상세 로그 팝업 및 재전송 (Redelivery)
- 특정 전송 내역을 클릭하면 요청 헤더/바디 및 수신 서버가 반환한 응답 헤더/바디를 대조 표시.
- **재전송 버튼:** 클릭 시, 저장된 기존 페이로드와 설정을 토대로 다시 `WebhookTask`를 채널에 인큐하여 재발송 수행.

---

## 6. Validation Strategy (Nyquist Validation Matrix)

- **V-01 (Async Dispatch):** 웹훅 발송을 트리거할 때 메인 API 스레드가 대기(Block)하지 않고 50ms 이내에 즉시 200/204 응답을 반환하는지 비동기 테스트.
- **V-02 (Signature Verification):** 발송된 웹훅의 헤더 `X-Aristokeides-Signature-256`에 담긴 해시가 설정된 Secret 키와 페이로드 바디를 기준으로 올바르게 생성되었는지 대조 테스트.
- **V-03 (Payload Adapters):** Slack/Discord 변환 엔진을 작동하여 생성된 JSON이 슬랙/디스코드의 수신 규격 포맷과 호환되는지 대조 단위 테스트.
- **V-04 (Redelivery Integrity):** 수동 재전송 시 `X-Aristokeides-Delivery`가 재발행되지만 본문과 원본 이벤트 속성이 원래 전송과 바이트 단위로 동일하게 복사되어 전송되는지 검증.
