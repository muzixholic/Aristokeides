# Phase 11: Homepage & Dashboard - Context

**Gathered:** 2026-06-08
**Status:** Ready for planning

<domain>
## Phase Boundary

비로그인 시 사용자를 위한 랜딩 페이지(Homepage)와 로그인 시 사용자를 위한 대시보드(Dashboard)를 제공하여, 프로젝트 접속 초기 진입 화면을 구축하고 사용자별 맞춤 화면(저장소 목록 등)을 제공합니다. 기존 404가 발생하던 루트(`/`) 경로를 정상적인 라우팅으로 분기하여 개선합니다.

</domain>

<decisions>
## Implementation Decisions

### Landing Page Content Strategy
- **D-01:** 기능(Git 호스팅, 이슈 트래커, 리뷰 등)을 시각적으로 설명하는 마케팅 섹션을 포함하여 구성합니다.
- **D-02:** 설명 섹션에는 Bootstrap Icons 등 경량화된 웹 아이콘 폰트를 활용하고 텍스트 설명 위주로 깔끔하게 구성합니다.

### Dashboard Repository Layout
- **D-03:** 로그인 사용자 진입 시 보이는 대시보드는 저장소 이름, 간단한 설명과 자물쇠 아이콘(비공개 시)이 포함된 카드 형태의 그리드 뷰(Grid/Card View)를 채택합니다.
- **D-04:** 각 리포지토리 카드에는 최근 업데이트 시간, 비공개 여부, 주요 언어 정도의 필수 메타데이터를 표시합니다.

### Root Routing & Redirection Logic
- **D-05:** 사용자가 루트 경로(`/`)로 접근할 경우, 서버 측에서 로그인 상태를 판별하여 비로그인이면 `/home`으로, 로그인이면 `/dashboard`로 명시적 302 리다이렉트 처리를 합니다.

### the agent's Discretion
- 랜딩 페이지의 구체적인 카피라이팅 및 UI 테마/색상 배치
- 카드 뷰(Grid/Card View)의 한 줄당 배치 개수(반응형 처리 방식)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

No external specs — requirements fully captured in decisions above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Layout Template:** 기존 Phase 10에서 다듬은 인증 화면 레이아웃과 공통 테마 자산(CSS 등)을 최대한 재사용합니다.

### Established Patterns
- **Authentication:** Phase 10에서 구축한 이중 인증 체계(Cookie + JWT)의 `User.Identity.IsAuthenticated` 상태를 활용하여 루트 경로 컨트롤러에서 리다이렉션을 처리합니다.

### Integration Points
- `HomeController` 또는 `RootController` (기본 `/` 매핑)
- 대시보드에서 `Aristokeides.Data` 의 `Repositories` (해당 사용자의 권한을 기준으로 필터링)와 연동.

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 11-homepage-dashboard*
*Context gathered: 2026-06-08*
