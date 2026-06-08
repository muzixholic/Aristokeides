# Phase 13: Layout & Navigation Polish - Research Notes

## 1. 현재 네비게이션 및 레이아웃 상태 분석

- **현재 네비게이션 구조:**
  - `MainLayout.razor` 내부에 단순 `<a>` 태그로 로고 및 회원 정보, 설정, 로그인, 회원가입 등의 링크가 정의되어 있습니다.
  - 현재 활성화된 페이지 경로에 따른 시각적 힌트(Active State)가 존재하지 않습니다.
- **반응형 상태:**
  - `app.css`에 기본적인 패딩만 적용되어 있으며, 브라우저가 극도로 좁아질 경우 네비게이션 바 우측 요소들이 겹치거나 가독성이 해쳐질 수 있습니다.
- **푸터(Footer) 부재:**
  - 현재 전체 레이아웃 하단에 저작권 표시나 Swagger 바로가기를 제공하는 푸터가 존재하지 않습니다.

## 2. 해결 방안 설계

### A. Blazor NavLink 및 Active 스타일 구현
- **NavLink 컴포넌트 활용:**
  - 단순 `<a>` 대신 `<NavLink>`를 사용하여 라우트 주소 일치 시 자동으로 `active` 클래스가 추가되도록 설정합니다.
  - 대시보드 링크: `<NavLink href="/dashboard" Match="NavLinkMatch.All" class="nav-link">대시보드</NavLink>`
  - 설정 링크: `<NavLink href="/settings" class="nav-link">설정</NavLink>`
- **CSS 스타일 구성 (`app.css`):**
  - `.navbar`와 `.nav-link` 관련 클래스를 추가하여 마진과 패딩을 조절합니다.
  - 활성화 상태 스타일 정의:
    ```css
    .nav-link.active {
        color: var(--accent) !important;
        border-bottom: 2px solid var(--accent);
        padding-bottom: 4px;
        font-weight: 600;
    }
    ```

### B. 글로벌 네비게이션 구조화 (D-01 의사결정 준수)
- **로그인 상태일 때:**
  - 좌측: 로고(`Aristokeides` -> `/` 경로) + **"대시보드"** NavLink 배치.
  - 우측: **"새 저장소 만들기"** 버튼(Accent 컬러 배경 + 흰 글씨) + `[Username] 님` 텍스트 + **"설정"** NavLink + **"로그아웃"** 버튼/링크 배치.
- **비로그인 상태일 때:**
  - 좌측: 로고(`Aristokeides` -> `/` 경로).
  - 우측: **"로그인"** 링크 + **"회원가입"** 링크 배치.

### C. 하이브리드 푸터 구현 (D-03 의사결정 준수)
- **푸터 마크업 (`MainLayout.razor`):**
  - `<main>` 하단에 `<footer>` 영역을 추가합니다.
  - 좌측: `© 2026 Aristokeides. All rights reserved.`
  - 우측 바로가기 링크 그룹:
    - API 문서 바로가기: `/swagger` (새 창 열기 `target="_blank"`)
    - 홈: `/home`
    - 대시보드: `/dashboard`

### D. CSS Flex Wrap 기반 반응형 처리 (D-04 의사결정 준수)
- **네비게이션 바 및 푸터 반응형 속성:**
  - `.navbar`와 `.footer`에 `display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: 16px;` 속성을 부여합니다.
  - 뷰포트 너비가 좁아져 공간이 부족하면 메뉴 항목들이 자연스럽게 아래 줄로 이동(wrap)되도록 설계합니다.
  - `@media (max-width: 640px)` 이하에서 네비게이션 바와 푸터를 가운데 정렬(`justify-content: center; text-align: center;`)하여 모바일 친화적으로 최적화합니다.
