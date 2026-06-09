---
title: "GitHub & Google OAuth2 소셜 로그인 구현"
phase: 18
wave: 2
depends_on: [18A-PLAN.md]
files_modified:
  - Aristokeides.Api/Aristokeides.Api.csproj
  - Aristokeides.Api/Models/User.cs
  - Aristokeides.Api/Data/AppDbContext.cs
  - Aristokeides.Api/Program.cs
  - Aristokeides.Api/Controllers/AuthController.cs
  - Aristokeides.Api/Components/Pages/Login.razor
autonomous: true
requirements:
  - "사용자는 별도 가입 절차 없이 GitHub 또는 Google 계정을 사용하여 회원가입 및 소셜 로그인을 수행할 수 있어야 한다."
  - "소셜 가입 사용자는 최초 가입 시 Reader 권한을 획득해야 하며, 기존 사용자는 자신의 프로필에서 소셜 연동을 진행할 수 있어야 한다."
---

# Plan 18B: GitHub & Google OAuth2 소셜 로그인 구현

## Objective

`Microsoft.AspNetCore.Authentication.Google` 및 `AspNet.Security.OAuth.GitHub` 패키지를 연동하여 소셜 로그인 환경을 마련하고, 데이터베이스에 소셜 매핑 정보(`UserSocialLogin` 엔터티)를 추가한다. 로그인 UI에 연동 버튼을 추가하고, OAuth2 콜백 컨트롤러를 통해 신규 사용자 자동 가입 또는 기존 이메일 매핑 로직을 구현한다.

## Tasks

<task id="18B-1">
<title>OAuth2 패키지 추가 및 UserSocialLogin 모델 구현</title>
<read_first>
- `Aristokeides.Api/Aristokeides.Api.csproj` — 프로젝트 패키지 구조 참조
- `Aristokeides.Api/Data/AppDbContext.cs` — DB 매핑 양식 참조
</read_first>
<action>
1. 소셜 로그인 연동을 위한 NuGet 패키지를 추가한다:
   - Command: `dotnet add Aristokeides.Api/Aristokeides.Api.csproj package Microsoft.AspNetCore.Authentication.Google`
   - Command: `dotnet add Aristokeides.Api/Aristokeides.Api.csproj package AspNet.Security.OAuth.GitHub`
2. `Aristokeides.Api/Models/UserSocialLogin.cs` 엔터티를 신규 생성한다:
   ```csharp
   namespace Aristokeides.Api.Models;

   public class UserSocialLogin
   {
       public int Id { get; set; }
       public int UserId { get; set; }
       public required string Provider { get; set; } // "GitHub", "Google"
       public required string ProviderKey { get; set; } // 외부 사용자 고유 ID
       public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

       public User User { get; set; } = null!;
   }
   ```
3. `AppDbContext.cs`에 `DbSet<UserSocialLogin>`을 등록하고 `OnModelCreating`에서 복합 유니크 인덱스 `(Provider, ProviderKey)` 및 외래키 연동 설정을 구성한다.
4. `EF Core` 마이그레이션(Sqlite, Postgres, Mysql 각각)을 추가하고 적용한다:
   - `dotnet ef migrations add AddUserSocialLogins --context SqliteAppDbContext -o Migrations/Sqlite`
   - `dotnet ef migrations add AddUserSocialLogins --context PostgresAppDbContext -o Migrations/Postgres`
   - `dotnet ef migrations add AddUserSocialLogins --context MysqlAppDbContext -o Migrations/Mysql`
</action>
<acceptance_criteria>
- 소셜 로그인 패키지가 정상 설치되고 컴파일 에러가 발생하지 않는다.
- 데이터베이스 마이그레이션이 각 프로바이더별로 정상 반영된다.
</acceptance_criteria>
</task>

<task id="18B-2">
<title>Program.cs 소셜 로그인 서비스 및 미들웨어 추가</title>
<read_first>
- `Aristokeides.Api/Program.cs` — 기존 `builder.Services.AddAuthentication` 체인 참조 (L37-71)
</read_first>
<action>
`Program.cs`의 인증 빌더 체인에 Google 및 GitHub 인증을 연결하도록 구성을 수정한다:

1. `appsettings.json` 설정에 다음 필드 구성을 추가한다:
   ```json
   "Authentication": {
     "Google": {
       "ClientId": "YOUR_GOOGLE_CLIENT_ID",
       "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
     },
     "GitHub": {
       "ClientId": "YOUR_GITHUB_CLIENT_ID",
       "ClientSecret": "YOUR_GITHUB_CLIENT_SECRET"
     }
   }
   ```
2. `Program.cs` 내 `AddAuthentication`에 다음 체인을 추가한다 (단, ClientId가 설정되지 않은 경우에도 앱 구동이 중단되지 않도록 조건부 체크 지원):
   ```csharp
   .AddGoogle(opts =>
   {
       opts.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "dummy";
       opts.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "dummy";
       opts.SignInScheme = "Cookies"; // 소셜 로그인 후 1차 쿠키 임시 저장
   })
   .AddGitHub(opts =>
   {
       opts.ClientId = builder.Configuration["Authentication:GitHub:ClientId"] ?? "dummy";
       opts.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"] ?? "dummy";
       opts.SignInScheme = "Cookies";
       opts.Scope.Add("user:email");
   });
   ```
</action>
<acceptance_criteria>
- `Program.cs`가 정상적으로 빌드된다.
- `Authentication:Google` 및 `Authentication:GitHub` 바인딩이 에러 없이 준비된다.
</acceptance_criteria>
</task>

<task id="18B-3">
<title>소셜 로그인 컨트롤러 엔드포인트 구현</title>
<read_first>
- `Aristokeides.Api/Controllers/AuthController.cs` — 기존 로그인/콜백 처리 흐름 확인
</read_first>
<action>
`AuthController.cs`에 소셜 로그인 챌린지 및 콜백 엔드포인트를 추가한다:

1. **외부 로그인 요청 엔드포인트 (`GET /api/auth/external-login`):**
   ```csharp
   [HttpGet("external-login")]
   public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] string? redirectUrl = "/")
   {
       var properties = new AuthenticationProperties { RedirectUri = Url.Action("ExternalLoginCallback", new { redirectUrl }) };
       return Challenge(properties, provider);
   }
   ```
2. **외부 로그인 콜백 엔드포인트 (`GET /api/auth/external-login-callback`):**
   - `HttpContext.AuthenticateAsync("Cookies")`를 통해 외부 로그인된 인증 정보(Identity/Claims)를 가져온다.
   - Claims에서 `ProviderKey` (예: NameIdentifier), `Email`, `Name` (사용자명) 등을 획득한다.
   - **사용자 매핑 로직:**
     - `UserSocialLogin` 테이블에 해당하는 `Provider`와 `ProviderKey` 매핑 존재 시: 해당 User 정보로 로그인 쿠키 발급.
     - 매핑이 없으나 동일한 `Email` 사용자가 이미 존재할 시: 신규 `UserSocialLogin` 연동 기록 추가 및 해당 User로 로그인 쿠키 발급.
     - 둘 다 없을 시: 신규 사용자 생성 (이메일 = external email, 사용자명 = external name 또는 고유 식별 명칭, 임의의 무작위 비밀번호 해시, Role="Reader") 후 `UserSocialLogin` 매핑 생성 및 신규 User로 로그인 쿠키 발급.
   - 로그인 완료 후 `redirectUrl`로 리다이렉트. (사용자에게 2FA가 활성화된 계정일 경우, 2FA 임시 세션 발급 후 `/login-2fa` 유도 로직 연동 적용)
</action>
<acceptance_criteria>
- `/api/auth/external-login`을 통해 특정 프로바이더(GitHub/Google) 로그인 페이지로 이동한다.
- 인증 성공 시 콜백 엔드포인트가 외부 클레임을 정확히 캡처한다.
- 신규 소셜 사용자가 가입될 때 권한 상승 위험 없이 `Role = "Reader"`로 안전하게 가입 처리된다.
- 이메일 중복이 존재하는 경우 신규 가입 대신 기존 계정에 소셜 연동 매핑이 자동 안전 매핑된다.
</acceptance_criteria>
</task>

<task id="18B-4">
<title>로그인 페이지 소셜 연동 UI 추가</title>
<read_first>
- `Aristokeides.Api/Components/Pages/Login.razor` — 기존 HTML 폼 구조
</read_first>
<action>
`Login.razor`를 수정하여 소셜 로그인 실행 버튼을 추가한다.

1. **소셜 로그인 구분선 및 버튼 추가:**
   - 기존 일반 로그인 버튼 하단에 "또는 소셜 계정으로 로그인" 구분선 추가.
   - GitHub 및 Google 로그인 버튼 배치 (시각적으로 정돈된 버튼 디자인 적용 - GitHub/Google 브랜드 컬러 및 아이콘 스타일).
   - 클릭 시 `/api/auth/external-login?provider=GitHub` 및 `/api/auth/external-login?provider=Google` 호출 연동 (a 태그 링크 또는 form action).
</action>
<acceptance_criteria>
- 로그인 화면에 GitHub 및 Google 로그인 버튼이 깔끔하게 표시된다.
- 버튼을 클릭하면 올바른 컨트롤러 엔드포인트로 유도된다.
</acceptance_criteria>
</task>

## must_haves

- 소셜 로그인 구현 시 소셜 공급자의 `ClientId` 및 `ClientSecret`은 하드코딩하지 않고 구성 파일(`appsettings.json` 등)에서 바인딩되어야 한다.
- 외부 계정을 연동해 자동으로 생성되는 계정은 이메일 고유성 검증을 거쳐야 한다.
- 외부 로그인 콜백을 통한 가입 시에도 2FA 조건 검증이 적절히 작동해야 한다.

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Models/UserSocialLogin.cs` | 신규 | 소셜 로그인 매핑 정보 엔터티 |
| `Aristokeides.Api/Data/AppDbContext.cs` | 수정 | `UserSocialLogin` DB 매핑 추가 |
| `Aristokeides.Api/Program.cs` | 수정 | Google/GitHub OAuth 인증 핸들러 미들웨어 설정 추가 |
| `Aristokeides.Api/Controllers/AuthController.cs` | 수정 | 외부 로그인 챌린지 및 콜백 연동 로직 추가 |
| `Aristokeides.Api/Components/Pages/Login.razor` | 수정 | 소셜 로그인 실행 버튼 UI 통합 |
