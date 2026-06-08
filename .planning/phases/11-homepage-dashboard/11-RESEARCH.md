# Phase 11: Homepage & Dashboard - Research Notes

## 1. 현재 상태 및 코드 컨텍스트
- **라우팅 (Routing)**: `Aristokeides.Api/Components/Routes.razor` 에 루트(`/`) 경로에 대한 매핑이 없어 접근 시 "Not found"가 발생합니다. `/` 경로를 가로채어 리다이렉트하는 컨트롤러 처리가 필요합니다.
- **데이터 모델 (Data Model)**: `Aristokeides.Api/Models/Repository.cs` 모델에는 `11-CONTEXT.md`에서 요구한 대시보드 카드 표시 항목인 `IsPrivate`(비공개 여부), `PrimaryLanguage`(주요 언어), `UpdatedAt`(최근 업데이트 시간) 필드가 없습니다.
- **UI 아키텍처**: 서버 사이드 렌더링(SSR) 모드의 Blazor 8(`Components/Pages` 하위 `.razor` 파일)를 사용 중입니다.
- **의존성 (Dependencies)**: 페이지의 `<head>` 태그는 `App.razor`에서 관리됩니다. 요구사항에 명시된 Bootstrap Icons를 사용하기 위해 CDN 링크 추가가 필요합니다.

## 2. 계획(Plan) 수립을 위한 핵심 구현 방안

### A. 모델 및 데이터베이스 스키마 업데이트
- **대상 파일**: `Aristokeides.Api/Models/Repository.cs`
- **구현 내용**: 대시보드 표시에 필요한 속성을 추가합니다.
  - `public bool IsPrivate { get; set; } = true;`
  - `public string? PrimaryLanguage { get; set; }`
  - `public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;`
- **마이그레이션**: EF Core 마이그레이션을 생성하고 적용해야 합니다. (*참고: 해당 필드들의 실제 데이터 수정 및 생성 폼은 Phase 12에서 구현되지만, Phase 11의 대시보드 화면 바인딩을 위해 스키마 업데이트가 선행되어야 합니다.*)

### B. 루트 라우팅 분기 처리 (`/`)
- **대상 파일**: `Aristokeides.Api/Controllers/RootController.cs` (신규)
- **구현 내용**: `[HttpGet("/")]` 엔드포인트를 처리하는 컨트롤러를 생성합니다. `User.Identity?.IsAuthenticated` 값을 확인하여 로그인 상태면 `Redirect("/dashboard")`로, 비로그인 상태면 `Redirect("/home")`으로 명시적 302 리다이렉트를 수행합니다.

### C. 랜딩 페이지 (`/home`)
- **대상 파일**: `Aristokeides.Api/Components/Pages/Home.razor` (신규)
- **구현 내용**: 비로그인 사용자를 위한 프로젝트 소개 페이지입니다. (`@page "/home"`) 기능(Git 호스팅, 이슈 트래커, 코드 리뷰 등)을 텍스트 위주로 깔끔하게 설명하는 섹션을 구성합니다.
- **설정 추가**: `Aristokeides.Api/Components/App.razor`의 `<head>` 영역에 Bootstrap Icons CDN 링크를 추가합니다. (`<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css">`)

### D. 대시보드 (`/dashboard`)
- **대상 파일**: `Aristokeides.Api/Components/Pages/Dashboard.razor` (신규)
- **구현 내용**: 로그인 사용자를 위한 저장소 목록 페이지입니다. (`@page "/dashboard"`, `@attribute [Authorize]` 적용)
- **데이터 조회**: 현재 사용자의 클레임(`ClaimTypes.NameIdentifier`)에서 ID를 가져와 접근 가능한 저장소 목록을 조회합니다. `DbContext.Repositories.Include(r => r.Owner).Where(r => r.OwnerId == userId).ToListAsync()`
- **UI 레이아웃**: CSS Grid를 사용하여 카드 뷰 형태로 배치합니다. (예: `display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 16px;`)
- **카드 내용**: 
  - 저장소 이름 (클릭 시 `/@repo.Owner!.Username/@repo.Name` 경로로 이동)
  - 자물쇠 아이콘 (`IsPrivate`가 true일 경우)
  - 저장소 설명
  - 최근 업데이트 시간 (`UpdatedAt`) 및 주요 언어 (`PrimaryLanguage`)
- **다음 페이즈 연동**: Phase 12에서 구현할 신규 저장소 생성 페이지로 가는 버튼 (`<a href="/repositories/new">New Repository</a>`)을 배치해 둡니다.
