# Phase 12: Repository Management UI - Context

**Gathered:** 2026-06-08
**Status:** Ready for planning

<domain>
## Phase Boundary

저장소 생성 및 설정 관리를 위한 웹 화면 구축. 신규 저장소 생성 폼(`/repositories/new`)과 기존 저장소의 기본 정보(이름, 설명, 가시성 등) 변경, 그리고 저장소 삭제 기능을 제공합니다.

</domain>

<decisions>
## Implementation Decisions

### 저장소 삭제 확인 방식
- **D-01:** 저장소 삭제 시 실수로 인한 방지를 위해 Gitea/GitHub 스타일의 안전 모달(Safe Deletion Modal)을 사용합니다. 사용자가 텍스트 창에 직접 저장소 이름(예: `username/reponame`)을 타이핑하여 입력된 이름과 실제 저장소 이름이 일치하는 경우에만 "삭제" 버튼이 활성화되어 클릭할 수 있습니다.

### 저장소 이름 중복 및 유효성 검증 방식
- **D-02:** 저장소 이름 및 가시성, 설명 등에 대한 입력 검증은 폼을 제출(Submit)하는 시점에 서버/DB 단에서 중복성 및 형식 검사를 수행하고, 문제가 있을 경우 에러 박스(Alert Box)를 폼 상단에 노출합니다. 실시간 API 연동 검사 대신 심플하고 확실한 제출 시점 검증 방식을 지향합니다.

### 설정 변경 시 피드백 방식
- **D-03:** 리포지토리 설정 페이지에서 이름이나 설명 변경을 완료하여 제출했을 때, 다른 화면으로 리다이렉트하지 않고 기존 설정 페이지에 그대로 머무릅니다. 대신 화면 상단에 녹색 톤의 성공 안내 박스(성공 메시지)를 띄워 저장이 완료되었음을 알립니다.

### 추가 고급 설정 범위
- **D-04:** 이번 페이즈(Phase 12)의 구현 범위는 핵심 기능인 이름, 설명, 가시성(IsPrivate) 변경 및 저장소 삭제 기능에 집중하며, 아카이브(Archive) 및 소유권 이전(Transfer Ownership) 등 더 고급 기능들은 scope에서 제외(Deferred)합니다.

### the agent's Discretion
- 모달 윈도우 레이아웃 및 팝업 트랜지션 애니메이션 디테일
- 삭제 완료 후 대시보드(`/dashboard`)로의 자동 리다이렉트 처리 방식
- 폼 검증 에러 발생 시 입력 필드 테두리 색상 강조(Red border) 등 세부 UI 효과

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project Scope & Goals
- [PROJECT.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/PROJECT.md) — 핵심 가치 및 범위 정의
- [REQUIREMENTS.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/REQUIREMENTS.md) — 사용자 스토리 정의

### Design Specifications
- [11-UI-SPEC.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/phases/11-homepage-dashboard/11-UI-SPEC.md) — 디자인 시스템 스펙 (Color, Typography, Spacing 토큰 및 Bootstrap Icons 사용)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Blazor `<EditForm>` 및 `<DataAnnotationsValidator>` 활용
- `MainLayout.razor` 및 글로벌 스타일 자산 (`app.css`)의 CSS 변수 활용 (`--dominant`, `--secondary`, `--accent`, `--destructive`)
- `Bootstrap Icons` (예: `bi-trash`, `bi-gear`, `bi-plus-lg`)

### Established Patterns
- `User.Identity.IsAuthenticated` 기반 세션 상태 검증 및 DbContext 직접 인젝션 활용 패턴 (`Settings.razor` 참조)

### Integration Points
- `RepositoriesController.cs` (저장소 생성 API) - 폼 동작과 통합되거나 DbContext를 직접 활용
- 대시보드(`Dashboard.razor`)의 "새 저장소 만들기" 링크 (`/repositories/new`)

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

- 저장소 아카이브(Archive) 기능
- 저장소 소유권 이전(Transfer Ownership) 기능

</deferred>

---

*Phase: 12-Repository-Management-UI*
*Context gathered: 2026-06-08*
