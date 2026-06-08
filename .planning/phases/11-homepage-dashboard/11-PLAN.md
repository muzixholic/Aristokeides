# Phase 11: Homepage & Dashboard - Plan

**Target Phase:** 11
**Status:** executed

## Step 1: Repository 모델 스키마 확장
- **File:** `Aristokeides.Api/Models/Repository.cs`
- **Action:** 
  - `IsPrivate` (bool, default true), `PrimaryLanguage` (string, nullable), `UpdatedAt` (DateTime, default UtcNow) 속성을 `Repository` 클래스에 추가합니다.
  - 변경 사항을 DB에 적용하기 위해 Entity Framework Core 마이그레이션 스크립트를 실행합니다.

## Step 2: 루트 리다이렉션 라우팅 처리
- **File:** `Aristokeides.Api/Controllers/RootController.cs` (신규 생성)
- **Action:** 
  - `[HttpGet("/")]` 엔드포인트를 구현합니다.
  - `User.Identity?.IsAuthenticated` 속성을 검사하여, 참일 경우 `/dashboard`로, 거짓일 경우 `/home`으로 명시적인 `Redirect` (302) 처리를 수행합니다.

## Step 3: 전역 아이콘 종속성 추가
- **File:** `Aristokeides.Api/Components/App.razor`
- **Action:** 
  - `<head>` 태그 내에 Bootstrap Icons CDN 링크 (`https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css`)를 삽입합니다.

## Step 4: 비로그인 사용자를 위한 랜딩 페이지 구현
- **File:** `Aristokeides.Api/Components/Pages/Home.razor` (신규 생성)
- **Action:** 
  - `@page "/home"` 라우트 지시어를 추가합니다.
  - 프로젝트의 주요 기능(Git 호스팅, 이슈 트래커, 코드 리뷰 등)을 시각적으로 설명하는 마케팅 섹션을 구현합니다.
  - `11-UI-SPEC.md`에 정의된 타이포그래피(Display 32px, Heading 24px) 및 여백(Spacing) 토큰을 적극 활용하여 깔끔하고 미니멀한 UI를 구성합니다.

## Step 5: 로그인 사용자를 위한 대시보드 구현
- **File:** `Aristokeides.Api/Components/Pages/Dashboard.razor` (신규 생성)
- **Action:** 
  - `@page "/dashboard"` 및 `@attribute [Authorize]` 지시어를 적용합니다.
  - `ApplicationDbContext`를 주입받아 접속 중인 사용자의 저장소(Repository) 목록을 데이터베이스에서 조회합니다.
  - 조회한 목록을 CSS Grid 레이아웃 (예: `grid-template-columns: repeat(auto-fill, minmax(300px, 1fr))`)을 사용하여 리포지토리 카드 뷰 형태로 표시합니다.
  - 각 저장소 카드에는 이름, 비공개 상태 자물쇠 아이콘(`IsPrivate`가 참인 경우), 최근 업데이트 시간, 주요 언어 등을 표시합니다. 저장소 이름 클릭 시 상세 경로(`/@repo.Owner!.Username/@repo.Name`)로 이동하도록 구현합니다.
  - 페이지 상단 우측에 "새 저장소 만들기" 링크 버튼(Primary CTA)을 배치합니다. 링크 경로는 `/repositories/new` 입니다.
  - 반환된 저장소가 0개일 경우, `11-UI-SPEC.md`에 정의된 문구("생성된 저장소가 없습니다", "아직 저장소를 생성하지 않았습니다...")를 Empty State 뷰로 출력합니다.
