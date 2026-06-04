# Phase 9: Advanced Review Workflow - Context

**Gathered:** 2026-06-04
**Status:** Ready for planning

<domain>
## Phase Boundary

이 단계는 Pull Request(PR)의 고급 리뷰 워크플로우를 구현합니다. 구체적으로, 사용자가 단일 댓글 즉시 작성 또는 "리뷰 시작"을 통한 여러 댓글 임시 보관(Pending) 후 일괄 제출(Submit review)할 수 있도록 지원하며, 새 커밋이 브랜치에 푸시되는 경우 변경 코드 영역의 기존 댓글을 "Outdated" 상태로 자동 전환 및 위치 밀림 보정(Line Shift)을 실행합니다. 또한 PR 내 미해결(Unresolved) 토론이 존재할 경우 병합(Merge)을 차단하며, PR 리뷰 액션(Comment, Approve, Request Changes)을 기록하고 신규 커밋 푸시 시 기존 승인(Approve) 상태를 자동으로 리셋하는 처리를 포함합니다.

</domain>

<decisions>
## Implementation Decisions

### 1. 임시 보관(Pending) 댓글 저장 방식 및 스키마 설계
- **D-01:** DB 테이블의 `IsPending` 컬럼 및 리뷰 세션 관리 — 임시 보관 중인 댓글은 브라우저 세션에 국한되지 않고 다중 장치 및 세션 간 유지를 보장할 수 있도록, `PullRequestReviewComment` 테이블(또는 신규 Review 엔터티)에 `IsPending` (bool) 컬럼을 추가하여 DB 서버 측에 안전하게 영속화한다.

### 2. 라인 번호 보정(Line Shift) 및 Outdated 판단 연산의 작동 시점
- **D-02:** Git Push 완료 직후 백그라운드 서비스에서 즉시 갱신 — 사용자가 PR 상세 페이지를 조회할 때의 로딩 성능 저하를 방지하기 위해, HTTP 또는 SSH 프로토콜을 통해 Git Push(git-receive-pack)가 성공적으로 완료되는 즉시 백그라운드 서비스에서 관련 PR들의 Git Diff를 분석하여 DB의 라인 번호를 보정하고 Outdated 여부를 미리 연산하여 DB에 갱신한다.

### 3. 신규 푸시 시 승인 취소(Reset Approval)의 동작 강도
- **D-03:** 기존의 모든 승인(Approve) 상태 자동 초기화 및 재승인 강제 — 코드 안정성을 확보하기 위해 소스 브랜치(PR 대상)에 신규 커밋이 푸시되어 브랜치 상태가 변경되는 즉시, 기존에 등록되어 있던 모든 리뷰어의 `Approve` 상태를 자동으로 초기화(취소)하여 전면 재검토를 강제한다.

### 4. 미해결 토론 병합 차단(Block Merge) 및 관리자 예외 정책
- **D-04:** 미해결 토론 존재 시 일반 사용자 병합 차단 및 관리자 강제 병합(Force Merge) 허용 — PR 내 해결되지 않은(Unresolved) 토론 스레드가 단 하나라도 존재하면 일반 사용자의 병합 처리를 엄격히 차단(Merge 버튼 비활성화 및 backend 예외 처리)하되, 관리자(Admin) 권한을 가진 사용자에 한해서는 예외적으로 경고 메시지 확인 후 강제 병합(Force Merge)할 수 있는 우회 옵션을 UI와 API 레벨에서 제공한다.

### 5. the agent's Discretion (에이전트 재량 사항)
- 라인 번호 보정 연산에 대한 세부 매핑 알고리즘 구체화 (Myers Diff 또는 기존 DiffParser 기반 Oid 간 Hunk 매핑)
- 승인 상태 초기화 시 웹 UI 상에서의 알림 배지 및 알림 메시지 노출 양식 설계
- UI 상에서 Pending 상태의 댓글 표시 디자인 및 일괄 제출 모달 인터페이스 세부 구성

</decisions>

<specifics>
## Specific Ideas

- 사용자는 PR 리뷰 시 `Comment`, `Approve`, `Request Changes` 라디오 버튼을 선택하여 의견을 일괄 제출할 수 있습니다.
- 관리자 권한을 가진 경우 병합 영역에 "해결되지 않은 토론이 존재하지만 강제로 병합합니다" 체크박스가 활성화되며, 체크 시에만 병합 버튼이 활성화되는 등의 안전장치를 둡니다.

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 요구사항 및 설계서
- [REQUIREMENTS.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/REQUIREMENTS.md) §라인 단위 코드 리뷰 및 머지 차단 — CODE-05, CODE-07, CODE-09, CODE-10, CODE-11 상세 요구사항
- [08-CONTEXT.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/phases/08-pr-inline-comments/08-CONTEXT.md) — 이전 단계 인라인 댓글 구현 설계 및 엔터티 설계 내역

### 코드 리뷰 및 PR 서비스
- [PullRequestService.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Services/PullRequestService.cs) — `MergePullRequestAsync`, `GetReviewCommentsAsync` 등 핵심 비즈니스 로직
- [AppDbContext.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Data/AppDbContext.cs) — EF Core 데이터베이스 컨텍스트

### Git Push 이벤트 처리 미들웨어 및 서비스
- [GitSmartHttpMiddleware.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs) — HTTP Git Push 완료 후 후속 작업 트리거 지점
- [SshCommandBridge.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Services/Ssh/SshCommandBridge.cs) — SSH Git Push 완료 후 후속 작업 트리거 지점

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **PullRequestService**: PR 상태 갱신, 병합 처리 비즈니스 로직 확장 가능.
- **DiffParser**: 두 Oid 간 Diff 변경 정보를 파싱하는 데 활용.
- **SshCommandBridge.cs** & **GitSmartHttpMiddleware.cs**: Push 이벤트 성공 판정 후 비동기로 후속 연산(`sigService.VerifyNewCommitsAsync` 방식)을 처리하는 기존 구현 패턴 재사용 가능.

### Established Patterns
- Blazor Server SSR (Interactive Server)을 활용한 UI 상태 동기화 및 렌더링.
- 비동기 백그라운드 태스크 패턴을 통한 Git Push 후처리 로직 위임.

### Integration Points
- `RepoPullRequestDetail.razor`: 리뷰 제출 패널 UI 컴포넌트 추가 및 머지 가능 여부 판단 로직 고도화.

</code_context>

<deferred>
## Deferred Ideas

- None — discussion stayed within phase scope.

</deferred>

---

*Phase: 09-advanced-review-workflow*
*Context gathered: 2026-06-04*
