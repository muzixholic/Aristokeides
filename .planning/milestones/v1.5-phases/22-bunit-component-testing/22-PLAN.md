# Phase 22 Plan: bUnit 기반 Blazor 컴포넌트 단위 테스트 환경 구축

본 계획서는 `Aristokeides.Tests` 테스트 프로젝트 내에 `bUnit` 기반의 컴포넌트 가상 렌더링 테스트 환경을 구성하고, 주요 웹 UI 화면 4종에 대한 단위 테스트 코드를 신규 작성하여 비즈니스 유효성 검증 및 폼 바인딩을 자동으로 검증하는 상세 계획을 기술합니다.

## 1. Context & Objectives
* **Context**: [22-CONTEXT.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/phases/22-bunit-component-testing/22-CONTEXT.md)
* **Goal**:
  * C# / xUnit 테스트 프로젝트 환경에 bUnit을 연동하고, 브라우저 없는 메모리 기반 Blazor UI 검증 테스트 셋을 마련한다.
  * 핵심 4대 화면(`Login`, `NewRepository`, `RepoIssueForm`, `Setup`)의 유효성 검증 메시지 노출 및 동적 컴포넌트 이벤트 동작을 완전 자동 검사한다.

## 2. Detailed Tasks

### Task 1: bUnit 기반 테스트 유틸리티 및 베이스 클래스 구성
* **내용**: bUnit 테스트 시 매번 반복되는 DI 컨테이너 서비스 등록과 InMemory DB 생성을 캡슐화하는 테스트 베이스 클래스(`BunitTestBase.cs`)를 작성합니다.
* **구현 세부사항**:
  * `Aristokeides.Tests` 내에 `BunitTestBase` 클래스 생성.
  * `TestContext` (bUnit) 상속 및 `IDisposable` 구현.
  * 생성자에서 임의의 식별자(Guid)를 가진 EF Core InMemory `AppDbContext`를 초기화하여 `TestContext.Services.AddSingleton(DbContext)`로 주입.
  * `Mock` 혹은 간단한 테스트용 Dummy 클래스를 활용해 `IssueService`, `SetupService`, `AdminSettingsService` 등을 구성하여 DI에 등록.
  * `TestContext.Services.AddAntiforgery()` 및 필요한 로컬라이징/설정 서비스 연동.

### Task 2: Login.razor 및 Setup.razor 단위 테스트 구현
* **내용**: 폼 검증과 상태 기반 단순 렌더링에 적합한 두 컴포넌트에 대한 테스트 코드를 구현합니다.
* **구현 세부사항**:
  * **[LoginTests.cs]**:
    * 쿼리스트링 파라미터 `Error`에 `"invalid_credentials"` 주입 후 렌더링 시, `"이메일 또는 비밀번호가 올바르지 않습니다."` 텍스트를 담은 에러 얼럿 박스가 표시되는지 검증.
    * 쿼리스트링 파라미터 `Registered`에 `"true"` 주입 후 렌더링 시, `"회원가입이 완료되었습니다."` 문구의 성공 알림 창이 나타나는지 검증.
  * **[SetupTests.cs]**:
    * 컴포넌트 최초 렌더링 시 기본 `SQLite` 프로바이더 관련 필드가 보이는지 검사.
    * 데이터베이스 프로바이더 선택 드롭다운(`select`)의 값을 `"PostgreSQL"`로 강제 변경(이벤트 트리거)했을 때, 포트 번호가 `5432`로 바뀌고 SQLite 전용 필드가 가려지는지 검증 (`OnDatabaseProviderChanged` 호출 검증).
    * `SetupViewModel` 폼에서 이메일 형식 오류, 암호 불일치 상태에서 제출 시 에러 라벨(`ValidationMessage`)에 오류 안내문구가 올바르게 렌더링되는지 검증.
    * 설치 성공 응답 시 `NavigationManager`가 `"/"` 경로로 `forceLoad: true`와 함께 이동 처리되는지 스파이 검증.

### Task 3: NewRepository.razor 및 RepoIssueForm.razor 단위 테스트 구현
* **내용**: 데이터 바인딩 및 인증 정보 바인딩이 얽혀 있는 두 핵심 대화형 컴포넌트의 비즈니스 규칙과 이벤트를 테스트합니다.
* **구현 세부사항**:
  * **[NewRepositoryTests.cs]**:
    * `Bunit.TestAuthorizationExtensions`를 사용해 테스트 컨텍스트에 가상 사용자(예: Username = `"tester"`, Role = `"Contributor"`) 인증 정보 설정.
    * 가상 DB(`AppDbContext`)에 해당 사용자가 Owner인 조직(`Organization`) 데이터 추가.
    * 렌더링 후 소유자 드롭다운(`#owner-selector`) 내 옵션 목록에 가상 사용자의 조직이 텍스트로 정상 포함되어 렌더링되는지 검증.
    * 동일한 소유자 명의로 이미 존재하는 저장소 이름을 기입하고 제출 시, `"이미 사용 중인 저장소 이름입니다:"` 문구를 포함한 에러 메시지가 표출되는지 검증.
  * **[RepoIssueFormTests.cs]**:
    * DB에 mock 리포지토리 및 mock 사용자 정보 등록.
    * 제목(`Title`) 인풋 박스를 비워 둔 채 `EditForm`의 `submit` 이벤트를 트리거했을 때, `Title is required` 유효성 에러 라벨이 렌더링되는지 검사.

## 3. Verification Plan

### 자동화된 로컬 테스트 실행
* `Aristokeides` 프로젝트 루트에서 PowerShell 또는 Bash 터미널을 열고 다음 명령어를 실행하여 전체 테스트 프로젝트의 동작 상태를 검증합니다.
  ```powershell
  dotnet test E:\Workspace\VisualC#\Aristokeides\Aristokeides.Tests\Aristokeides.Tests.csproj --filter "FullyQualifiedName~Aristokeides.Tests"
  ```
* **성공 기준**: 새로 작성한 bUnit 관련 테스트 케이스들이 실패 없이 녹색(Green)으로 100% 통과할 것.
