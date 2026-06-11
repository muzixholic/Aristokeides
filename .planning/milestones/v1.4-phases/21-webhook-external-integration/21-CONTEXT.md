# Phase 21: 웹훅 및 외부 연동 - Context

**Gathered:** 2026-06-10
**Status:** Ready for planning

<domain>
## Phase Boundary

저장소 이벤트(Push, Issue, PR 등) 발생 시 등록된 웹훅 URL로 JSON 페이로드를 비동기 전송하는 웹훅 인프라를 구축합니다. 개별 저장소 수준의 웹훅 관리(CRUD) UI, 웹훅 전송 이력(Delivery Log) 관리 및 실패 건 수동 재전송(Redelivery) 기능, 슬랙(Slack)/디스코드(Discord) 맞춤형 페이로드 전송 기능 등을 완전하게 제공합니다.

</domain>

<decisions>
## Implementation Decisions

### 웹훅 비동기 전송 아키텍처
- **D-01:** 웹훅 전송은 메인 API 응답에 병목을 주지 않도록 메모리 기반 큐 Channel(`System.Threading.Channels.Channel`)과 백그라운드 워커 서비스(`IHostedService`)를 이용해 완벽히 비동기식으로 처리합니다.

### 웹훅 보안 서명 (Signature)
- **D-02:** 웹훅 등록 시 비밀값(Secret Key)을 설정할 수 있게 하며, 전송 시 요청 바디의 전체 페이로드에 대해 HMAC-SHA256 서명을 생성하여 요청 헤더 `X-Aristokeides-Signature-256`에 담아 보냅니다.

### 전송 이력 및 재전송 (Redelivery)
- **D-03:** 각 웹훅 요청의 요청 헤더/바디, 응답 헤더/바디, 상태 코드, 전송 소요 시간을 데이터베이스 `WebhookDelivery` 테이블에 저장하여 UI에서 상세 확인하고, 관리자가 재요청(Redeliver) 단추를 눌러 동일 페이로드를 다시 큐에 적재하도록 구현합니다.

### 슬랙 및 디스코드 페이로드 어댑터
- **D-04:** 웹훅 등록 시 전송 타입(`WebhookType`)으로 `Generic`, `Slack`, `Discord`를 지원합니다. `Slack`과 `Discord` 타입의 경우, 범용 JSON 페이로드를 각 메신저의 인커밍 웹훅 규격(예: `text`, `attachments`, `embeds`)에 부합하도록 즉석 변환하여 전송합니다.

### the agent's Discretion
- 웹훅 재전송 시 원래의 전송 이벤트 식별 정보(`X-Aristokeides-Delivery`)를 유지하여 수신자가 재전송 건임을 인지하도록 유도합니다.
- 데이터베이스 테이블의 오래된 전송 기록은 성능 유지를 위해 추후 별도 가비지 컬렉션(GC)을 두거나 보관 기한 제한(예: 30일)을 명시하는 것을 허용합니다.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Specs & References
- [GitHub Webhooks Guide](https://docs.github.com/en/webhooks) — 웹훅 서명 헤더 및 재전송 사양 표준 벤치마킹
- [Slack Incoming Webhooks](https://api.slack.com/messaging/webhooks) — 슬랙 페이로드 변환 규격 참조
- [PROJECT.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/PROJECT.md) — 핵심 요구사항 정의

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- [RepositoryCreationBackgroundWorker.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Services/RepositoryCreationBackgroundWorker.cs): Channel 기반의 비동기 백그라운드 워커 패턴을 참조하여 `WebhookBackgroundWorker`를 정의할 수 있습니다.
- [AppDbContext.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Data/AppDbContext.cs): 엔티티 매핑 추가 시 활용합니다.

### Integration Points
- 저장소 내 커밋 Push 완료 시점 ([GitSmartHttpMiddleware.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs) 및 `SshCommandBridge.cs` 등)에 웹훅 트리거 이벤트를 비동기 디스패치하는 연동점.
- 이슈 및 PR 관련 비즈니스 로직 연동점 ([IssueService.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Services/IssueService.cs), [PullRequestService.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Services/PullRequestService.cs)).

</code_context>

<specifics>
## Specific Ideas

- **웹훅 트리거 이벤트 유형:**
  - `push`: 코드 푸시 발생 시 (커밋 목록, 브랜치명 포함)
  - `issue`: 이슈 생성, 수정, 종료, 댓글 등록 시
  - `pull_request`: PR 생성, 변경, 병합, 리뷰 댓글 등록 시

</specifics>

<deferred>
## Deferred Ideas

- 웹훅 전송 실패 시 백오프(Exponential Backoff)를 포함한 자동 재시도 메커니즘 — 이번 페이즈는 수동 재전송에 집중하고 자동 재시도는 추후 개선사항으로 이관.

</deferred>

---

*Phase: 21-웹훅 및 외부 연동*
*Context gathered: 2026-06-10 via discuss-phase substitution*
