# Phase 8: PR Inline Comments - Context

**Gathered:** 2026-06-04
**Status:** Ready for planning

<domain>
## Phase Boundary

이 단계는 Pull Request의 파일 변경 Diff 화면에서 특정 코드 라인에 대해 마우스 호버 시 `+` 버튼을 노출하고, 클릭 시 마크다운 글쓰기 및 실시간 미리보기가 지원되는 인라인 댓글 작성 창을 표시하는 기능을 구축합니다. 작성된 인라인 댓글은 파일 경로, 원본/대상 라인 번호, 변경 상태 및 Hunk 컨텍스트를 포함하여 별도의 DB 테이블(`PullRequestReviewComment`)에 저장되고 새로고침 없이 화면에 즉각 렌더링되며, 스레드 형식의 답글 달기 및 대화 해결(Resolve) / 재개(Reopen) 상태 관리를 지원합니다.

</domain>

<decisions>
## Implementation Decisions

### 1. 데이터 스키마 및 저장 구조
- **D-01:** 별도의 `PullRequestReviewComment` 테이블 생성 — 인라인 댓글은 파일 경로, 라인 번호, `DiffHunk`, 해결 상태(`IsResolved`) 등 고유의 도메인 책임을 가지므로 일반 이슈 댓글(`IssueComment`)과 분리하여 별도 테이블로 모델링한다.
- **D-02:** 작성자 삭제 시 Restrict / PR 삭제 시 Cascade — 풀 리퀘스트(Issue)가 삭제되면 관련 인라인 댓글도 연쇄 삭제(Cascade)되나, 작성자(User)가 삭제되는 경우는 무결성을 위해 댓글 삭제를 제한(Restrict)한다. (기존 `Issue.CreatorId` 패턴과의 일관성 유지)

### 2. 라인 매핑 및 위치 지정 방식
- **D-03:** 원본/대상 라인 번호 및 라인 타입 모두 저장 — `FilePath`와 함께 `OldLineNumber`(기존 파일 라인, 선택), `NewLineNumber`(새 파일 라인, 선택) 및 `LineType`(추가 `+` / 삭제 `-` / 유지 ` `) 정보를 모두 저장한다. 이는 삭제 행에 대한 댓글 작성 지원 및 Phase 9에서 다룰 라인 보정(Line Shift)의 핵심 메커니즘으로 활용된다.
- **D-04:** Hunk 컨텍스트 전체 저장 — 댓글 대상 행 주변의 Diff Hunk 전체(@@ 헤더 하위 변경 블록)를 텍스트로 보관하여, 향후 소스 코드가 수정되거나 파일이 바뀌더라도 당시 작성된 코드 맥락을 UI에 안정적으로 표시할 수 있게 한다.

### 3. 대화 스레드 및 답글 관리 방식
- **D-05:** `ParentId` 기반 self-referencing 구성 — `PullRequestReviewComment` 내에 Nullable인 `ParentId` 필드를 추가하여 답글이 최상위(부모) 댓글을 참조하도록 설계한다.
- **D-06:** 부모 댓글에서 `IsResolved` 상태 관리 — 대화의 해결 여부(Resolve)는 최상위(부모) 댓글의 `IsResolved` 속성을 통해 나타내며, 해당 상태에 따라 UI 상에서 스레드 전체를 접고 펼치도록 제어한다.
- **D-07:** 모든 프로젝트 참여자에게 상태 변경 권한 허용 — PR을 볼 수 있는 권한을 가진 모든 참여자(작성자 및 리뷰어)가 자유롭게 토론을 해결(Resolve)하거나 다시 재개(Reopen)할 수 있도록 허용한다.

### 4. 인라인 댓글 작성 및 표시 UX
- **D-08:** Diff 라인 아래 컴포넌트 삽입 및 탭 방식 마크다운 에디터 — 라인 호버 시 `+` 버튼이 노출되고 클릭 시 라인 아래에 작성 폼이 삽입된다. Write 탭과 Preview 탭 간의 전환을 통해 마크다운 문법의 실시간 미리보기를 제공한다.
- **D-09:** NuGet 패키지 `Markdig` 사용 — C# 표준 마크다운 파서 패키지인 `Markdig`을 프로젝트에 설치하여, Blazor Server에서 마크다운 텍스트를 HTML로 파싱하고 `MarkupString`을 통해 안전하게 화면에 출력한다.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 요구사항
- `.planning/REQUIREMENTS.md` §라인 단위 코드 리뷰 — CODE-04, CODE-06, CODE-08 상세 요구사항

### 코드 리뷰 및 PR 도메인 모델
- `Aristokeides.Api/Models/PullRequest.cs` — 풀 리퀘스트 엔터티
- `Aristokeides.Api/Models/Issue.cs` — 이슈 엔터티 (PR과 1:1 관계)
- `Aristokeides.Api/Models/IssueComment.cs` — 기존 이슈 댓글 모델
- `Aristokeides.Api/Data/AppDbContext.cs` — DbContext 모델 및 테이블 관계 설정 파일

### Git Diff 및 PR 서비스
- `Aristokeides.Api/Services/PullRequestService.cs` — `GetPullRequestDiffAsync` (LibGit2Sharp 기반 Diff 생성 기능 내장)

### UI 컴포넌트
- `Aristokeides.Api/Components/Pages/RepoPullRequestDetail.razor` — 현재 PR 정보 및 Diff 텍스트를 렌더링하고 있는 화면 (인라인 댓글 UI 삽입 대상)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **PullRequestService.GetPullRequestDiffAsync**: LibGit2Sharp를 이용하여 두 트리 간의 비교 패치를 얻고 문자열로 반환하는 로직을 제공하므로, 인라인 댓글 구현 시 이를 라인 단위 구조로 파싱해 활용할 수 있다.
- **AppDbContext**: 기존 엔터티 등록 형태를 참고하여 `DbSet<PullRequestReviewComment>`를 추가하고, OnModelCreating에서 복합키 및 DeleteBehavior 정책을 반영한다.

### Established Patterns
- **Blazor Server SSR (Interactive Server)**: 실시간 렌더링 상태 업데이트 및 마크다운 에디터 프리뷰 탭 처리에 적합한 Blazor Server 방식을 유지하여 개발한다.
- **DeleteBehavior**: 작성자 삭제에 대해서는 무결성 보호를 위해 `DeleteBehavior.Restrict`를 사용하고, 부모 엔터티(PR/Issue) 삭제 시에는 `DeleteBehavior.Cascade`를 적용한다.

### Integration Points
- **RepoPullRequestDetail.razor**:
  - 기존의 `<code class="language-diff">@diffContent</code>` 렌더링 부분을 파일별, 라인별 구조로 쪼개어 테이블 형식 등으로 렌더링하도록 변경해야 한다.
  - 마우스 호버 이벤트를 활용하여 각 행 옆에 `+` 버튼을 제공한다.
  - 해당 행 아래에 `PullRequestReviewComment`의 스레드 목록 및 신규 작성 입력 컴포넌트를 삽입해야 한다.

</code_context>

<specifics>
## Specific Ideas

- GitHub의 코드 리뷰 화면처럼 각 코드 라인 왼쪽에 위치한 행 번호 셀이나 코드 시작 부분에 마우스 호버 시 파란색의 `+` 버튼을 작게 표시한다.
- 작성 폼은 `Write` 탭과 `Preview` 탭이 존재하며, `Preview` 탭 선택 시 서버에서 `Markdig`으로 렌더링한 마크다운 HTML이 실시간으로 노출된다.
- 토론 해결 시 "Resolved" 배지와 함께 스레드가 축소되어 가려지며, "Show conversation" 링크 버튼을 누르면 접혀있던 토론 내용이 다시 확장되어 노출되게 설계한다.

</specifics>

<deferred>
## Deferred Ideas

- **Phase 9으로 연기된 기능 (Advanced Review Workflow):**
  - "리뷰 시작" 및 일괄 제출(Submit review) 기능 (CODE-05)
  - 새 커밋 푸시 시 코드 코멘트 위치 자동 보정(Line Shift) 및 Outdated 처리 (CODE-07)
  - 미해결 토론 존재 시 머지 차단 (CODE-09)
  - PR 승인(Approve) 및 변경 요청(Request Changes) 워크플로우 (CODE-10, CODE-11)

</deferred>

---

*Phase: 8-PR Inline Comments*
*Context gathered: 2026-06-04*
