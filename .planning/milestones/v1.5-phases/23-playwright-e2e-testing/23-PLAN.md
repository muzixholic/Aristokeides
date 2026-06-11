# Phase 23 Plan: Playwright 기반 E2E UI 테스트 자동화 환경 구축 및 시나리오 테스트

본 계획서는 `Aristokeides.Tests` 프로젝트 내에 `Playwright` E2E 브라우저 자동화 테스트 환경을 세팅하고, 실제 구동되는 Kestrel 웹 서버 및 격리 데이터베이스 위에서 핵심 비즈니스 흐름 4종을 검증하는 실행 계획을 수립합니다.

## 1. Context & Objectives
* **Context**: [23-CONTEXT.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/phases/23-playwright-e2e-testing/23-CONTEXT.md)
* **Goal**:
  * `Microsoft.Playwright` 및 `Microsoft.AspNetCore.Mvc.Testing` 패키지 설치.
  * 실제 네트워크 포트를 오픈하여 대기하는 `PlaywrightWebApplicationFactory<Program>` 구성.
  * 임시 격리 DB(`e2e_test.db`)의 매 실행 시 마이그레이션 리셋 구조 확보.
  * 4대 흐름(인증/세션, 저장소 관리, PR 및 코드리뷰, 이슈 및 칸반) E2E 테스트 자동화 코드 구현.

## 2. Detailed Tasks

### Task 1: Playwright 및 Mvc.Testing 패키지 추가 및 드라이버 연동
* **내용**: E2E 테스트 구동에 필요한 프로젝트 종속성 패키지들을 `Aristokeides.Tests.csproj` 에 추가하고 브라우저 바이너리(Chromium) 획득 가이드를 작성합니다.
* **구현 세부사항**:
  * `Aristokeides.Tests.csproj` 파일에 다음 패키지 레퍼런스 추가:
    * `Microsoft.AspNetCore.Mvc.Testing` (웹 호스트 가동용)
    * `Microsoft.Playwright` (브라우저 제어용)
  * 빌드가 끝난 후 드라이버가 준비될 수 있도록 Playwright CLI 설치 명령(`playwright install` 혹은 `dotnet build` 후 `pwsh bin/Debug/net10.0/playwright.ps1 install` 등)을 실행하는 셸 명령어 검증.

### Task 2: PlaywrightWebApplicationFactory<TProgram> 테스트 팩토리 구현
* **내용**: 메모리 내의 모크 서버가 아니라 실제 포트를 점유하고 구동되는 통합 웹 호스트 팩토리를 작성합니다.
* **구현 세부사항**:
  * `Aristokeides.Tests` 폴더 내에 `PlaywrightWebApplicationFactory.cs` 파일 생성.
  * `WebApplicationFactory<TProgram>`를 상속받는 클래스 구현.
  * `CreateHost` 오버라이드: `IHostBuilder` 빌드 시 `UseUrls("http://localhost:5002")`를 통해 Kestrel 포트를 오픈하고, `IHost.Start()`를 명시적으로 실행하여 실제 포트 수신 대기를 기동.
  * `ConfigureWebHost` 오버라이드:
    * `ConfigureAppConfiguration`를 가로채어 임시 데이터베이스 정보(`Database:Provider` = `"SQLite"`, `Database:ConnectionString` = `"Data Source=e2e_test.db"`)를 주입하도록 구성.
    * `IsInstalled` 설정값을 주입하여, 필요 시 최초 Setup 화면 테스트 또는 기설치 모드로 분기될 수 있도록 제어 필드 노출.
  * 팩토리 기동(Initialize) 시 기존 `e2e_test.db` 파일을 삭제하고, 새로운 dbContext 인스턴스를 통해 `Database.MigrateAsync()`를 수행하여 E2E 테스트 환경 격리 준비.

### Task 3: Playwright E2E 시나리오 테스트 구현
* **내용**: 4가지 비즈니스 케이스를 완전 자동 검증하는 E2E 테스트 코드(`PlaywrightE2eTests.cs`)를 구현합니다.
* **구현 세부사항**:
  * `Aristokeides.Tests` 폴더 내에 `PlaywrightE2eTests.cs` 생성.
  * `IClassFixture<PlaywrightWebApplicationFactory<Program>>` 또는 개별 테스트 단위 팩토리 상속을 통해 백그라운드 서버와 연동.
  * **테스트 시나리오 1: 최초 Setup 및 초기 부팅**:
    * `IsInstalled`가 false인 상태에서 `/setup` 진입 시 초기 설정 화면이 뜨며, 관리자 정보 입력 후 제출 시 "/" 리다이렉트 및 `e2e_test.db`에 어드민 사용자가 생성되는지 E2E 검증.
  * **테스트 시나리오 2: 로그인/로그아웃 및 세션**:
    * `/login` 진입 후 생성한 어드민 계정으로 로그인 시도.
    * 성공 시 대시보드 화면(`/`)으로 정상 진입하는지 확인 및 세션 정상 바인딩 검증.
    * 로그아웃 클릭 시 정상적으로 `/login` 페이지로 돌아오는지 검증.
  * **테스트 시나리오 3: 저장소 생성 및 관리**:
    * 로그인 상태에서 "새 저장소 만들기" 버튼 클릭 ➡️ 저장소명("e2e-project") 기입 ➡️ 생성 버튼 클릭.
    * 생성 완료 후 `/{username}/e2e-project` 주소로 리다이렉트되고 저장소 디렉토리가 정상 생성되는지 검증.
  * **테스트 시나리오 4: 이슈 트래커 및 PR 코드 리뷰**:
    * 저장소 내의 이슈 신규 작성 및 칸반 보드 렌더링 검증.
    * 풀 리퀘스트 생성 버튼 클릭 후 생성 흐름이 깨지지 않고 완수되는지 E2E 흐름 검증.

## 3. Verification Plan

### Playwright 드라이버 설치 및 테스트 작동
* 빌드 완료 후 Playwright 브라우저 종속성을 설치하기 위해 PowerShell에서 다음 명령을 실행합니다.
  ```powershell
  dotnet build E:\Workspace\VisualC#\Aristokeides\Aristokeides.Tests\Aristokeides.Tests.csproj
  & "$HOME/.nuget/packages/microsoft.playwright/1.48.0/driver/tools/playwright.ps1" install
  ```
  *(또는 실제 다운로드된 Playwright 패키지 드라이버 경로에 입각하여 install 구동)*
* 그 후 전체 테스트 구동:
  ```powershell
  dotnet test E:\Workspace\VisualC#\Aristokeides\Aristokeides.Tests\Aristokeides.Tests.csproj --filter "FullyQualifiedName~Aristokeides.Tests.Playwright"
  ```
* **성공 기준**: 4개 E2E 테스트 시나리오가 모두 **PASSED** 처리될 것.
