# Phase 21: 웹훅 및 외부 연동 - Validation Strategy

**Date:** 2026-06-10
**Status:** Completed

## 1. Verification Dimensions

### Dimension 1: Functional Correctness (기능적 정확성)
- 웹훅 관리 CRUD API의 작동 여부를 검증합니다.
- `Generic`, `Slack`, `Discord` 타입에 대해 알맞은 페이로드 형식으로 변환 및 발송되는지 테스트 코드로 증명합니다.

### Dimension 2: Security & Isolation (보안 및 격리)
- 등록하려는 웹훅 URL이 내부망 사설 IP 대역 또는 로컬 루프백 대역일 경우 검증을 통해 차단(SSRF 방지)되는지 체크합니다.
- API 조회 시 `Secret` 문자열이 평문 노출되지 않고 마스킹(`********`) 처리되거나 빈 값 형태로 응답이 주어지는지 검증합니다.
- HMAC-SHA256 해시 서명 검증 코드를 작성하여 위조 방지가 정확한 키로 작동하는지 대조합니다.

### Dimension 3: Robustness & Data Integrity (복구력 및 무결성)
- 수신자 서버가 오프라인이거나 잘못된 주소로 발송될 때 스레드가 멈추지 않고 예외 처리가 작동하여 `WebhookDelivery` 테이블에 실패 상태(예: HTTP 0, 에러 로그 수집)로 안전하게 기록되는지 확인합니다.
- 응답 본문이 너무 클 경우 64KB 선에서 잘리는지(Truncation) 검사하여 DB 메모리 오버런 방지 여부를 판단합니다.

### Dimension 4: Async Performance (비동기 처리 성능)
- 이벤트 트리거 시 채널 인큐가 즉각 일어나며, 호출 스레드가 HTTP 발송 완료까지 블록되지 않는지 확인합니다.

---

## 2. Test Execution Plan

### 2.1. Integration & Unit Test Cases (`Aristokeides.Tests/WebhookTests.cs`)
- `Test_Webhook_Crud`: 웹훅 CRUD API 및 유효성 검사 테스트.
- `Test_Webhook_Ssrf_Prevention`: 루프백/사설 IP 등록 시도 시 유효성 검증 실패(400 Bad Request) 처리 검증.
- `Test_Webhook_Secret_Masking`: 웹훅 조회 시 Secret 키 마스킹 노출 필터링 검증.
- `Test_Webhook_Signature_Generation`: HMAC-SHA256 서명 헬퍼가 규격 서명 문장(예: `sha256=...`)을 계산해 내는지 검사.
- `Test_Webhook_Adapter_Slack`: 범용 페이로드가 Slack 메시지 구조로 포맷팅 변환되는지 대조.
- `Test_Webhook_Adapter_Discord`: 범용 페이로드가 Discord 메시지 임베드 구조로 포맷팅 변환되는지 대조.
- `Test_Webhook_Async_Enqueue`: 채널 인큐 시 대기 없이 신속히 반환되는지 성능 테스트.
- `Test_Webhook_Redelivery`: 재전송 요청 시 Delivery UUID는 새로 갱신되나, 바디 페이로드는 원본 그대로 동반하여 발송 큐에 재진입하는지 검사.
