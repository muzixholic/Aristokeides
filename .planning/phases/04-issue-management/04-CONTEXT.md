# Phase 4: Issue Management - Context

**Gathered:** 2026-05-29
**Status:** Ready for planning

<domain>
## Phase Boundary

이슈 생성/수정/닫기 및 칸반 보드 렌더링 기능 구현. 각 이슈는 특정 저장소에 종속되며, 커스텀 컬럼을 이용해 상태를 관리합니다.
</domain>

<decisions>
## Implementation Decisions

### 1. 이슈 ID 체계 (Issue ID System)
- **D-01:** 저장소별 순차 ID 사용 (예: `LocalId`). 저장소 내에서 1번부터 시작하는 ID를 부여하여 GitHub/GitLab과 일관된 UX를 제공합니다. 이슈 생성 시 해당 저장소의 최대 ID값을 찾아 +1 하는 방식으로 구현합니다.

### 2. 칸반 보드 상태(컬럼) (Kanban Columns)
- **D-02:** 커스텀 상태(컬럼)를 허용합니다. `Repository`에 종속되는 상태 테이블(예: `IssueState` 또는 `BoardColumn`)을 별도로 두고, 저장소 생성 시 기본적으로 "To Do", "In Progress", "Done" 상태를 시딩(Seed)합니다.

### 3. 칸반 보드 UI 조작 (Board Interactivity)
- **D-03:** Blazor 서버 사이드 이벤트를 이용한 HTML5 네이티브 드래그 앤 드롭을 구현합니다. 무거운 외부 JavaScript 라이브러리 없이 직관적인 칸반 조작을 지원합니다.

### 4. 이슈 부가 정보 (Issue Metadata)
- **D-04:** 칸반 보드의 활용도를 높이기 위해 이슈에 담당자(Assignee) 지정 기능을 포함합니다. `User` 테이블과 연관관계를 맺어 사용자 식별을 지원합니다.

### the agent's Discretion
나머지 세부적인 DB 스키마 설계(이슈와 컬럼 간의 매핑)와 Blazor 컴포넌트 분리(칸반 보드 렌더링 등)는 요원의 판단에 따릅니다.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

No external specs — requirements fully captured in decisions above.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `User` 모델 (담당자 기능에 사용)
- `Repository` 모델 (이슈, 칸반 컬럼이 종속됨)
- 기존 Blazor App 라우팅 및 UI 프레임워크

### Established Patterns
- DB 연동: `AppDbContext`와 EF Core (Phase 1, 2)
- 뷰 렌더링: Blazor SSR / InteractiveServer 방식 활용 (Phase 3)

### Integration Points
- 라우팅: `/{username}/{repo}/issues`, `/{username}/{repo}/issues/{localId}` 등에 연동
- 저장소 생성 프로세스: 저장소 생성 시 기본 칸반 컬럼을 함께 추가하는 로직 통합
</code_context>

<specifics>
## Specific Ideas
- 커스텀 상태 관리를 위해 DB에 `IssueStatus` (또는 `KanbanColumn`) 엔티티를 추가하고 `RepositoryId`와 컬럼 노출 `Order`(순서) 필드를 가지도록 합니다.
</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope
</deferred>

---

*Phase: 4-Issue Management*
*Context gathered: 2026-05-29*
