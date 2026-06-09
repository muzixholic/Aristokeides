# Phase 18B Summary: GitHub & Google OAuth2 소셜 로그인 구현

## Objective
`Microsoft.AspNetCore.Authentication.Google` 및 `AspNet.Security.OAuth.GitHub` 패키지를 연동하여 소셜 로그인 환경을 마련하고, 데이터베이스에 소셜 매핑 정보(`UserSocialLogin` 엔터티)를 추가했습니다. 로그인 UI에 연동 버튼을 추가하고, OAuth2 콜백 컨트롤러를 통해 신규 사용자 자동 가입 또는 기존 이메일 매핑 로직을 안전하게 처리하도록 구현했습니다.

---

## 작업 수행 내용

### 1. OAuth2 관련 NuGet 패키지 설치
- `Microsoft.AspNetCore.Authentication.Google` (v10.0.8) 패키지 추가 완료.
- `AspNet.Security.OAuth.GitHub` (v10.0.0) 패키지 추가 완료.

### 2. UserSocialLogin 모델 및 데이터베이스 매핑 구성
- [UserSocialLogin.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/UserSocialLogin.cs) 파일을 생성하여 소셜 연동 정보를 저장할 엔터티를 정의했습니다.
- [User.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/User.cs) 엔터티에 `ICollection<UserSocialLogin> SocialLogins` 네비게이션 프로퍼티를 추가했습니다.
- [AppDbContext.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Data/AppDbContext.cs)에 `DbSet<UserSocialLogin>`을 추가하고 `OnModelCreating` 메소드 내에 `(Provider, ProviderKey)` 복합 유니크 인덱스 및 Cascade 삭제 조건을 설정했습니다.
- SQLite, PostgreSQL, MySQL을 위한 각각의 EF Core 마이그레이션(`AddUserSocialLogins`)을 생성하고, 로컬 SQLite 데이터베이스 파일에 업데이트를 반영했습니다.

### 3. Program.cs 소셜 로그인 서비스 구성
- [Program.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Program.cs)의 인증 체인 내에 `.AddGoogle(...)`과 `.AddGitHub(...)` 설정을 구현했습니다.
- `appsettings.json` 설정에 클라이언트 ID 및 시크릿 템플릿 필드를 추가하고, 설정값이 제공되지 않거나 개발용 플레이스홀더 문자열일 경우 애플리케이션 기동 중단을 방지하기 위한 폴백 처리("dummy" 값 사용) 로직을 연동했습니다.

### 4. AuthController 소셜 로그인 엔드포인트 구현
- [AuthController.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Controllers/AuthController.cs) 내부에 다음 엔드포인트를 구현했습니다:
  - `GET /api/auth/external-login`: 특정 소셜 프로바이더로 Challenge를 전송하여 로그인 페이지로 유도합니다.
  - `GET /api/auth/external-login-callback`: 외부 소셜 인증 완료 후 Claims 정보를 수신하여 매핑/가입 절차를 처리합니다.
- **안전한 가입 및 로그인 매핑 규칙 적용:**
  - 이미 가입되어 소셜 매핑 정보가 존재하는 사용자는 즉시 로그인 처리합니다.
  - 소셜 정보는 없지만 해당 이메일을 사용하는 기존 사용자가 존재할 시, 기존 사용자 계정에 해당 소셜 매핑을 자동 연동한 후 로그인 처리합니다.
  - 중복 사용자나 이메일이 없는 경우, 이메일 고유성 및 유니크한 사용자명 생성을 보장하며 임의의 무작위 비밀번호 및 기본 `Reader` 역할을 할당하여 신규 가입 처리합니다.
  - 로그인 성공 후에도 해당 사용자가 2FA(이중 인증) 활성화 상태일 경우 임시 2FA 세션(`amr = 2fa_pending`)을 발행하고 `/login-2fa`로 강제 유도합니다.

### 5. 로그인 UI 버튼 추가
- [Login.razor](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Components/Pages/Login.razor) 파일 내 일반 로그인 폼 하단에 "또는 소셜 계정으로 로그인" 구분선과 Google, GitHub 로그인 버튼을 추가했습니다.
- 시각적으로 깔끔한 디자인(브랜드 아이덴티티 색상 및 SVG 로고 포함)을 구성하고 각각 `/api/auth/external-login?provider=Google` 및 `/api/auth/external-login?provider=GitHub` 경로로 연동했습니다.

### 6. 검증을 위한 유닛 테스트 작성
- [OAuthTests.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Tests/OAuthTests.cs)를 생성하고 다음 시나리오를 검증하는 테스트들을 작성했습니다:
  - 외부 로그인 요청 시 적합한 ChallengeResult가 생성되는지 검증
  - 소셜 매핑 정보가 존재하는 유저의 성공적인 소셜 로그인 및 쿠키 발행 검증
  - 소셜 매핑이 없는 기존 유저가 같은 이메일로 접근할 시 소셜 계정 자동 연동 및 로그인 검증
  - 일치하는 유저가 없는 경우 `Reader` 역할과 고유 Username/Email을 기반으로 신규 가입 및 로그인 연동 검증
  - 소셜 로그인 성공 유저가 2FA 활성화 상태인 경우 임시 2FA 세션 발급 및 `/login-2fa` 이동 검증
- `dotnet test` 명령을 수행하여 모든 테스트가 정상 통과함을 확인했습니다.

---

## Verification Results
- 테스트 통과 결과: `Total: 63, Passed: 63, Failed: 0, Skipped: 0`
- 정상 빌드 및 실행 완료.
