# Phase 13: Layout & Navigation Polish - Plan

**Target Phase:** 13
**Status:** draft

## Step 1: 전역 스타일시트 확장 (app.css)
- **File:** `Aristokeides.Api/wwwroot/css/app.css`
- **Action:** 
  - 글로벌 스타일 변수 및 네비게이션 바, 푸터 클래스 관련 설정을 확장합니다.
  - 활성화 상태 스타일 정의: `.nav-link`가 `active` 클래스를 획득했을 때 글자 색상을 `var(--accent)`로 설정하고 밑줄(`border-bottom: 2px solid var(--accent);`)을 추가하는 규칙을 작성합니다.
  - 네비게이션용 Accent 버튼 스타일(`.btn-nav-accent`)을 정의합니다 (배경 Accent 색상, 패딩, 둥근 모서리 등).
  - 글로벌 푸터 스타일(`.footer`) 및 내부 링크 아이템 배치를 정의합니다.
  - 모바일 해상도(`max-width: 640px`) 대응 미디어 쿼리를 추가하여 헤더와 푸터가 중앙 정렬되고 수직으로 흐르도록 콤팩트 레이아웃을 최적화합니다.

## Step 2: MainLayout 레이아웃 마크업 수정 (MainLayout.razor)
- **File:** `Aristokeides.Api/Components/MainLayout.razor`
- **Action:** 
  - 기존의 단순 `<a>` 링크들을 Blazor `<NavLink>` 컴포넌트로 변경합니다.
  - 로그인 상태일 때 좌측 로고 우측에 **대시보드** 바로가기 NavLink를 노출합니다.
  - 로그인 상태일 때 우측 회원 정보 영역 옆에 콤팩트한 **새 저장소 만들기** 링크 버튼(`.btn-nav-accent`)을 추가합니다.
  - 레이아웃 최하단에 하이브리드 푸터(`<footer>`)를 생성하여 저작권 명세와 함께 **Swagger 바로가기(`/swagger`)** 및 주요 경로 링크들을 포함시킵니다.
  - 전체 레이아웃 구조가 깨지지 않도록 Flex-Wrap 속성을 지닌 Navbar 및 Footer 레이아웃 구조를 검증합니다.

## Step 3: 빌드 및 전체 레이아웃 정합성 검증
- **Action:** 
  - `dotnet build`를 실행하여 컴파일 오류가 없는지 최종 점검합니다.
  - `dotnet test`로 기존 테스트 깨짐이 없는지 검증합니다.
  - 로그인 세션 유지 시 헤더에 설정 탭 링크 및 대시보드 링크가 활성화 힌트(밑줄)와 함께 잘 정렬되는지 시각적 수동 검증을 수행합니다.
