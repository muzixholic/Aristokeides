---
title: "외부 메신저 연동 및 저장소 이벤트 웹훅 트리거 적용"
phase: 21
wave: 3
depends_on: ["21B"]
files_modified:
  - Aristokeides.Api/Services/Webhook/WebhookService.cs
  - Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs
  - Aristokeides.Api/Services/IssueService.cs
  - Aristokeides.Api/Services/PullRequestService.cs
autonomous: true
requirements:
  - "Slack 및 Discord 인커밍 웹훅 전송 포맷에 부합하도록 이벤트별 페이로드 변환기(Payload Adapter)를 구축한다."
  - "HTTP Push 발생 시점 및 SSH Push 처리부, 이슈(Issue) 및 Pull Request 비즈니스 로직 수행부에 웹훅 발송 트리거를 연결하여 실시간 연동을 실현한다."
---

# Plan 21C: 외부 메신저 연동 및 저장소 이벤트 웹훅 트리거 적용

## Objective

범용 웹훅 기능에서 더 나아가 Slack 및 Discord Incoming Webhook 규격을 자체 지원하는 어댑터를 설계하여 외부 메신저 채널과의 유기적인 연동을 실현합니다. 또한 소스 코드의 실제 Push 완료 시점 및 이슈 생성/댓글, PR 변경 등 협업 도구 이벤트 발생 시 웹훅 비동기 디스패치 메서드(`TriggerWebhookAsync`)를 적절히 호출하도록 기존 비즈니스 서비스들을 연동합니다.

## Tasks

<task id="21C-1">
<title>Slack & Discord 페이로드 어댑터 구현</title>
<read_first>
- `Aristokeides.Api/Services/Webhook/WebhookService.cs` — 기존 웹훅 비즈니스 로직 참조
</read_first>
<action>
1. `WebhookService.cs` 내부에 Slack 및 Discord 형식 메시지 변환기 구현:
   - **Slack Payload 변환:**
     - 전달할 EventType 및 페이로드 데이터를 받아 Slack Incoming Webhook 규격(`{ "text": "마크다운 메시지" }`)으로 변환합니다.
     - Push 이벤트의 경우 커밋 작성자, 브랜치명, 커밋 요약 메시지를 포함시킵니다.
     - Issue 이벤트의 경우 제목, 내용 일부, 작성자, 이슈 링크 주소를 가독성 있게 조합합니다.
   - **Discord Payload 변환:**
     - Discord의 `{ "content": "메시지" }` 규격 혹은 Embed 구조로 변환합니다.
     - 각 메신저에 호환되는 마크다운 문법(예: 볼드 `**`, 링크 `[텍스트](URL)`)으로 메시지 템플릿을 정의합니다.
2. `WebhookBackgroundWorker.cs`의 발송 로직 초입에서 대상 웹훅의 `WebhookType`이 `Slack` 또는 `Discord`일 경우, 해당 페이로드 가공 메서드를 통과시켜 최종 Body 본문을 치환한 후 HTTP 요청을 보냅니다.
</action>
<acceptance_criteria>
- 단위 테스트를 통해 `Generic` 페이로드 데이터가 Slack/Discord 용 문자열 포맷 JSON으로 정상 파싱 및 가공된다.
</acceptance_criteria>
</task>

<task id="21C-2">
<title>코드 Push 완료 시점 웹훅 트리거 연동</title>
<read_first>
- `Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs` — HTTP Push 성공 시 후처리 태스크 호출부 참조
- `Aristokeides.Api/Services/Ssh/SshCommandBridge.cs` — SSH 커밋 Push 처리 완료부 참조 (존재 시)
</read_first>
<action>
1. `GitSmartHttpMiddleware.cs` 수정:
   - HTTP Push가 성공하고 refs 업데이트가 정상 완료된 시점(라인 221~254 부근 후처리 태스크 안)에 `WebhookService`를 주입 또는 Resolve하여 `push` 웹훅 이벤트를 트리거합니다.
   - 페이로드에는 변경된 refs 명세(브랜치), 이전 OID, 신규 OID, 그리고 해당 커밋들에 속하는 상세 해시 및 커밋 메시지 정보 등을 조회하여 포함시킵니다.
2. `SshCommandBridge.cs` (또는 SSH Push 서비스) 내 SSH Git Push 완료 처리 로직이 있다면, 동일하게 `push` 웹훅 디스패치(`TriggerWebhookAsync`)를 추가 연동합니다.
</action>
<acceptance_criteria>
- Git HTTP Push가 완료된 즉시 백그라운드 발송 큐에 push 이벤트 태스크가 등록된다.
</acceptance_criteria>
</task>

<task id="21C-3">
<title>이슈 및 PR 라이프사이클 웹훅 연동</title>
<read_first>
- `Aristokeides.Api/Services/IssueService.cs` — 이슈 생성/수정/댓글 비즈니스 메서드 확인
- `Aristokeides.Api/Services/PullRequestService.cs` — PR 생성/수정/병합 비즈니스 메서드 확인
</read_first>
<action>
1. `IssueService.cs` 수정:
   - 이슈 신규 생성(`CreateIssueAsync` 등), 상태 수정(Open -> Closed), 이슈 댓글 생성 시점에 `WebhookService.TriggerWebhookAsync`를 호출하여 `issue` 이벤트를 트리거합니다.
2. `PullRequestService.cs` 수정:
   - PR 생성, 변경, 병합(Merge) 및 PR 코드 리뷰 댓글이 작성되는 시점에 `WebhookService.TriggerWebhookAsync`를 호출하여 `pull_request` 이벤트를 트리거합니다.
</action>
<acceptance_criteria>
- 웹 브라우저나 API를 통해 새로운 이슈를 작성하거나 댓글을 올릴 때 실시간으로 웹훅 발송 비동기 큐에 작업이 적재되는지 검증된다.
</acceptance_criteria>
</task>

## must_haves

- 외부 연동 페이로드 변환 시 NullReferenceException 등의 방어 코드를 내장하여, 예기치 않은 데이터 형식 누락 시에도 웹훅 컨슈머 스레드가 뻗지 않도록 조치해야 한다.
- 메신저 API 발송을 위한 최종 Content-Type은 `application/json` 포맷을 준수해야 한다.

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Services/Webhook/WebhookService.cs` | 수정 | Slack & Discord 페이로드 포맷 변환 로직 추가 |
| `Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs` | 수정 | HTTP Push 성공 후 웹훅 비동기 발송 트리거 연동 |
| `Aristokeides.Api/Services/IssueService.cs` | 수정 | 이슈 생성/종료/댓글 시점 웹훅 연동 |
| `Aristokeides.Api/Services/PullRequestService.cs` | 수정 | PR 생성/병합/리뷰 시점 웹훅 연동 |
