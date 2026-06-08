# Phase 13: Layout & Navigation Polish - Context

**Gathered:** 2026-06-08
**Status:** Ready for planning

<domain>
## Phase Boundary

글로벌 레이아웃 디자인 개선 및 네비게이션 구조화. 네비게이션 바 링크 추가, 활성화 링크 표시(Accent 글자색 + 밑줄), 하이브리드 푸터 추가(저작권 + Swagger + 바로가기 링크), 그리고 CSS Flex Wrap 기반 반응형 대응을 포함합니다.

</domain>

<decisions>
## Implementation Decisions

### 글로벌 네비게이션 바 링크 구성
- **D-01:** 로그인 상태일 때, 네비게이션 바 좌측 영역(로고 "Aristokeides" 우측)에 "대시보드(Dashboard)" 바로가기 링크를 배치합니다. 네비게이션 바 우측 영역에는 "새 저장소 만들기" 링크 버튼(Accent 색상 기반)을 콤팩트하게 배치하여 저장소 생성 접근성을 제고합니다.

### 네비게이션 활성화 상태 표시 스타일
- **D-02:** 현재 활성화된 페이지 경로(예: `/dashboard`, `/settings` 등)에 매핑되는 네비게이션 링크는 글자 색상을 Accent 컬러(`#2563EB`)로 바꾸고, 하단에 2px 굵기의 Accent 밑줄(Underline)을 활성화하여 시각적 명확성을 보장합니다.

### 하이브리드 푸터(Footer) 구성
- **D-03:** 페이지 하단에 글로벌 푸터(Footer)를 추가합니다. 푸터에는 콤팩트한 저작권 정보("© 2026 Aristokeides. All rights reserved.")와 함께 개발자용 Swagger API 문서 바로가기 링크(`/swagger`), 홈 바로가기 링크(`/home`), 대시보드 바로가기 링크(`/dashboard`)를 깔끔하게 정렬하여 노출합니다.

### 반응형 모바일 대응 방식
- **D-04:** 모바일 및 태블릿 등 좁은 뷰포트 환경에서 네비게이션 메뉴 및 푸터 요소가 겹치는 것을 막기 위해, CSS Flex-Wrap 레이아웃을 전격 활용합니다. 화면 너비가 좁아지면 JS 햄버거 메뉴를 여는 방식 대신 자연스럽게 요소가 아래 줄로 개행(Flex Wrap)되고 여백이 조절되도록 구현하여 Blazor Server SSR의 경량성을 극대화합니다.

### the agent's Discretion
- 네비게이션 바와 푸터의 테두리 선 굵기 및 그림자(Shadow) 적용 여부
- 브라우저 크기에 따른 패딩(Padding) 및 여백(Margin)의 미세 조정 방식
- 활성화 상태를 감지하기 위해 Blazor `NavLink` 컴포넌트의 `Match="NavLinkMatch.Prefix"` 또는 라우트 주소 매칭 로직의 상세 구현

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project Scope & Goals
- [PROJECT.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/PROJECT.md) — 핵심 가치 및 범위 정의
- [REQUIREMENTS.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/REQUIREMENTS.md) — 사용자 스토리 정의

### Design Specifications
- [11-UI-SPEC.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/phases/11-homepage-dashboard/11-UI-SPEC.md) — 디자인 시스템 스펙 (Color, Typography, Spacing 토큰)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `MainLayout.razor` (기존 네비게이션 바 정의 위치)
- `app.css` (글로벌 스타일시트)
- Blazor `NavLink` 컴포넌트 (URL 경로와 자동으로 활성화 클래스를 매칭하는 내장 컴포넌트)

### Established Patterns
- `AuthorizeView`를 통한 로그인 상태 분기 (`MainLayout.razor` 참조)

### Integration Points
- `MainLayout.razor` 파일의 마크업 수정 (네비게이션 바 및 하단 푸터 영역 삽입)
- `wwwroot/css/app.css` 파일 수정 (반응형 모바일 미디어 쿼리 및 NavLink 활성화 스타일 `active` 클래스 정의)

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

- 모바일 전용 토글형 슬라이드 햄버거 메뉴 (차후 모바일 앱 스타일로 고도화 시 재논의)

</deferred>

---

*Phase: 13-Layout-Navigation-Polish*
*Context gathered: 2026-06-08*
