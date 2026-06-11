# Phase 22 Context: bUnit 기반 Blazor 컴포넌트 단위 테스트 환경 구축

이 문서에는 Phase 22에서 구현할 Blazor UI 컴포넌트 단위 테스트의 설계 결정 사항과 테스트 대상을 기록합니다. 이 결정 사항은 후속 계획 수립 및 구현 작업의 명확한 기준이 됩니다.

## 1. Phase Goals

* `Aristokeides.Tests` 프로젝트에서 `bUnit`을 연동하여 Blazor 컴포넌트의 가상 렌더링 및 이벤트를 검증하는 단위 테스트 인프라 확립.
* 핵심 컴포넌트 4종(`Login`, `NewRepository`, `RepoIssueForm`, `Setup`)에 대한 렌더링, 폼 입력 유효성 검사, 이벤트 처리 상태 자동화 테스트 구현.

## 2. Core Decisions

### D-22-01: 데이터베이스 및 의존성 서비스 모킹 전략
* **데이터베이스**: 컴포넌트가 의존하는 DB 컨텍스트(`AppDbContext`)는 EF Core InMemory DB(`Microsoft.EntityFrameworkCore.InMemory`)를 사용해 실제 DB 연결과 가깝게 시뮬레이션한다.
* **비즈니스 서비스**: `IssueService`, `SetupService`, `AdminSettingsService` 등 복잡한 비즈니스 로직을 동반하는 서비스는 인터페이스 모크(Mock) 또는 bUnit 테스트 컨텍스트(`TestContext.Services`)에 가상 인스턴스로 등록하여 주입한다.
* **인증 상태**: bUnit의 내장 인증 헬퍼(`Bunit.TestAuthorizationExtensions`)를 사용해 테스트 진행 시 로그인 상태(Authorized) 및 비인증 상태(Anonymous)를 동적으로 전환하며 검증한다.

### D-22-02: 단위 테스트 대상 핵심 컴포넌트 범위
* **[Login.razor](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Components/Pages/Login.razor)**:
  * 비어 있는 값 입력 후 제출 시 폼 검증(Validation) 메시지 노출 여부 검증.
  * 올바른 데이터 입력 시 로그인 트리거 시뮬레이션.
* **[NewRepository.razor](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Components/Pages/NewRepository.razor)**:
  * 소유자(User / Organization) 선택 드롭다운에 따른 UI 렌더링 변화 검증.
  * 올바른 정보 입력 후 저장소 생성 이벤트 정상 호출 여부 검증.
* **[RepoIssueForm.razor](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Components/Pages/RepoIssueForm.razor)**:
  * 제목(Title)이 비어 있을 때 오류 메시지 정상 렌더링 여부 검증.
  * 마크다운 프리뷰 기능이 있는 경우 프리뷰 토글 동작 검증.
* **[Setup.razor](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Components/Pages/Setup.razor)**:
  * 최초 구동 시 표시되는 데이터베이스 프로바이더 선택 및 연결 문자열 검증 로직 검사.
  * 설치 완료 후 대시보드로의 정상 이동(NavigationManager) 여부 모킹 검증.

## 3. Gray Areas Resolved

| Gray Area | Selected Option | Rationale |
| :--- | :--- | :--- |
| DB 모킹 여부 | **EF Core InMemory DB 사용** | 실제 데이터 바인딩 시 복잡한 EF Core 질의와의 충돌을 방지하고 통합 성격의 UI 유효성 검사를 온전히 수행하기 위함 |
| 대상 컴포넌트 범위 | **Login, NewRepository, RepoIssueForm, Setup 4종** | 인증, 저장소 설정, 협업 기능 및 초기 부팅 등 애플리케이션의 중추적인 진입 장벽에 해당하는 화면들을 우선 검증 |

## 4. Next Steps

* [ ] 슬래시 커맨드 `/gsd-plan-phase 22`를 실행하여 구체적인 테스트 구현 시나리오와 스텝별 계획을 작성합니다.
