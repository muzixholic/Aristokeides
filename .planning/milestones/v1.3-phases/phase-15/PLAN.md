# Phase 15 Plan: 최초 설치 관리자 (Setup Wizard) 구현

## 1. 🎯 Objective
- 애플리케이션 최초 실행 시, 사용자가 데이터베이스를 선택하고 최초의 관리자(Admin) 계정을 생성할 수 있는 **설치 관리자(Setup Wizard)** 기능을 제공합니다.
- 설치가 완료되지 않은 상태에서는 모든 일반 요청을 `/setup` 경로로 리다이렉트하여 보안을 유지하고 필수 설정을 강제합니다.

## 2. 📝 Tasks

### Task 1: 초기화(설치) 여부 판단 로직 및 Middleware 구현
- `appsettings.json`에 `"IsInstalled": false` 플래그를 추가합니다.
- `SetupRedirectMiddleware`를 구현합니다:
  - `IConfiguration["IsInstalled"]` 값이 `true`이면 다음 미들웨어로 넘깁니다.
  - `false`일 때 요청 경로가 `/setup` 이거나 정적 파일(`.css`, `.js` 등)인 경우 통과시킵니다.
  - 그 외의 모든 요청은 `/setup`으로 `Redirect` 합니다.
- `Program.cs`의 HTTP 파이프라인(가급적 정적 파일 미들웨어 직후, 인증/인가 전)에 이 미들웨어를 등록합니다.

### Task 2: `/setup` 웹 UI(Blazor Component) 구현
- `Aristokeides.Api/Components/Pages/Setup.razor` 페이지를 생성합니다.
- **UI 폼 구성**:
  - **Database 설정 섹션:** SQLite, PostgreSQL, MySQL 중 선택 
    - (SQLite 선택 시 경로나 파일명 등 간단한 정보만 요구)
    - (PostgreSQL/MySQL 선택 시 Host, Port, Username, Password, Database Name 입력 폼 노출)
  - **관리자 계정 섹션:** 관리자 Username, Email, Password, Password Confirm 입력 폼
- 폼 데이터를 바인딩할 `SetupViewModel` 모델을 만들고 데이터 유효성 검사(Data Annotations)를 적용합니다.

### Task 3: Setup 저장 및 DB 마이그레이션 실행 로직
- 폼 제출 시 실행할 백엔드 로직(`SetupService` 또는 폼의 이벤트 핸들러)을 구현합니다.
- **설정 저장:** 사용자가 입력한 DB 정보를 바탕으로 `ConnectionString`을 조립하여 `appsettings.json` 파일을 수정하고 저장합니다 (JSON 파싱 및 쓰기 방식 활용).
- **런타임 마이그레이션:** 임시로 구성된 DB 연결 정보를 바탕으로 해당 Provider의 `DbContext` 인스턴스를 생성하고 `dbContext.Database.MigrateAsync()`를 호출하여 스키마를 생성합니다.
- **관리자 생성:** 마이그레이션이 완료되면 폼에서 입력받은 정보로 `User` 테이블에 Role이 `"Admin"`이고, BCrypt로 해싱된 암호를 가진 최초의 계정을 `INSERT` 합니다.
- 완료 후 `IsInstalled`를 `true`로 저장하고, 앱을 리다이렉트 하거나 재시작 안내를 띄웁니다.

## 3. 🔍 Verification
- [ ] `appsettings.json`의 `IsInstalled`가 `false`일 때, `/` 나 `/dashboard` 로 접속하면 자동으로 `/setup`으로 이동하는지 확인한다.
- [ ] `/setup` 페이지에서 폼 유효성 검사가 정상 작동하는지 확인한다.
- [ ] Setup 양식을 채우고 제출했을 때, 선택한 데이터베이스에 스키마가 정상적으로 생성되고 관리자 계정이 들어가는지 확인한다.
- [ ] 완료 후 `IsInstalled`가 `true`로 바뀌고 기존 미들웨어 차단이 해제되어 로그인 화면(`/login`)으로 접근 가능한지 확인한다.
