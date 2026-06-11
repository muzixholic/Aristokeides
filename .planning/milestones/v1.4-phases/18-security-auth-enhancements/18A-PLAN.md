---
title: "2FA TOTP 코어 로직 및 활성화/로그인 연동"
phase: 18
wave: 1
depends_on: []
files_modified:
  - Aristokeides.Api/Models/User.cs
  - Aristokeides.Api/Data/AppDbContext.cs
  - Aristokeides.Api/Controllers/AuthController.cs
  - Aristokeides.Api/Components/Pages/Settings.razor
  - Aristokeides.Api/Components/Pages/Login.razor
  - Aristokeides.Api/Components/Pages/Login2Fa.razor
  - Aristokeides.Api/Services/TwoFactorService.cs
autonomous: true
requirements:
  - "사용자는 계정의 보안 강화를 위해 TOTP 기반 2단계 인증(2FA)을 활성화할 수 있어야 한다."
  - "2FA가 활성화된 계정은 로그인 시 비밀번호 입력 완료 후 OTP 번호 검증을 통과해야 최종 로그인 처리가 되어야 한다."
---

# Plan 18A: 2FA TOTP 코어 로직 및 활성화/로그인 연동

## Objective

`Otp.NET` 패키지를 추가하여 Base32 비밀키 생성 및 TOTP 코드 검증 코어 기능을 구현하고, 사용자 프로필 설정(`Settings.razor`)에 2FA 활성화 UI(QR 코드 표시 및 검증)를 추가한다. 또한 로그인 프로세스(`Login.razor`, `AuthController.cs`)를 확장하여 2FA 활성 사용자는 임시 인증 상태 쿠키를 부여하고 `/login-2fa` 페이지로 리다이렉트하여 다단계 인증을 강제하도록 연동한다.

## Tasks

<task id="18A-1">
<title>2FA 지원을 위한 패키지 설치 및 User 모델 확장</title>
<read_first>
- `Aristokeides.Api/Aristokeides.Api.csproj` — 프로젝트 패키지 구조 참조
- `Aristokeides.Api/Models/User.cs` — 기존 User 모델 속성 확인
- `Aristokeides.Api/Data/AppDbContext.cs` — DB 구성 확인
</read_first>
<action>
1. `Otp.NET` 패키지를 추가한다:
   - Command: `dotnet add Aristokeides.Api/Aristokeides.Api.csproj package Otp.Net`
2. `User.cs` 모델에 2FA 관련 프로필 속성을 추가한다:
   ```csharp
   public bool IsTwoFactorEnabled { get; set; } = false;
   public string? TwoFactorSecret { get; set; }
   public string? TwoFactorRecoveryCodes { get; set; } // Comma-separated or JSON
   ```
3. `AppDbContext.cs` 내 `OnModelCreating`에서 User 엔터티 매핑을 구성한다 (선택 사항 - 기본 매핑으로 충분하나 명시적으로 비밀키 최대 길이 설정 등 구성 가능).
4. `EF Core` 마이그레이션(Sqlite, Postgres, Mysql 각각)을 추가하고 적용한다:
   - `dotnet ef migrations add AddUserTwoFactorColumns --context SqliteAppDbContext -o Migrations/Sqlite`
   - `dotnet ef migrations add AddUserTwoFactorColumns --context PostgresAppDbContext -o Migrations/Postgres`
   - `dotnet ef migrations add AddUserTwoFactorColumns --context MysqlAppDbContext -o Migrations/Mysql`
</action>
<acceptance_criteria>
- `Otp.Net` 패키지가 정상적으로 복구/빌드된다.
- User 모델에 `IsTwoFactorEnabled`, `TwoFactorSecret`, `TwoFactorRecoveryCodes` 속성이 존재한다.
- 데이터베이스 마이그레이션 파일이 `Migrations/` 하위의 SQLite, Postgres, MySQL 폴더에 정상 생성된다.
</acceptance_criteria>
</task>

<task id="18A-2">
<title>TwoFactorService 구현</title>
<read_first>
- `Aristokeides.Api/Services/` — 기존 서비스들 구조 참조 (예: SetupService.cs 등)
</read_first>
<action>
`Aristokeides.Api/Services/TwoFactorService.cs`를 새로 생성하여 다음 기능을 제공하는 싱글톤 혹은 범위(Scoped) 서비스를 정의한다:

1. **Secret Key 생성:** `KeyGeneration.GenerateRandomKey(20)`를 사용해 임의의 20바이트 키를 생성하고 이를 Base32 문자열로 변환하는 함수.
2. **TOTP 코드 검증:** 사용자 비밀키(Base32)와 입력된 6자리 코드를 받아 현재 시간 기준 유효한지 검증하는 함수 (`Totp` 클래스의 `VerifyTotp` 사용). 허용 오차 범위(시간 차이 대응)로 이전/이후 1개 시간 스텝(30초) 감안.
3. **복구 코드 생성:** 임의의 10자 문자열(예: `XXXX-XXXX-XXXX`)을 10개 만들어 배열로 반환하고, 저장 시 해시 처리하거나 평문 쉼표 조합으로 저장 및 검증하는 함수.
4. **Program.cs에 등록:** `builder.Services.AddScoped<TwoFactorService>();`
</action>
<acceptance_criteria>
- `TwoFactorService` 클래스가 정의되고 `Program.cs`에 의존성 주입 등록된다.
- Base32 비밀키를 안전하게 생성하고 디코딩할 수 있다.
- OTP 코드의 시간 보정을 지원하는 검증 로직이 구현된다.
- 10개의 복구 코드를 자동 생성 및 검증하는 유틸리티가 정상 작동한다.
</acceptance_criteria>
</task>

<task id="18A-3">
<title>Settings.razor에 2FA 활성화 UI 추가</title>
<read_first>
- `Aristokeides.Api/Components/Pages/Settings.razor` — 현재의 탭 탐색기 및 CSS 구조 확인
- `Aristokeides.Api/Controllers/AuthController.cs` — 현재 사용자 식별 획득 패턴 참조
</read_first>
<action>
`Settings.razor`를 수정하여 2FA 활성화/비활성화 기능을 연동한다.

1. **탭 네비게이션 확장:**
   - 탭 종류에 "Security & 2FA" 탭 추가.
2. **2FA 활성화 폼 구성 (InteractiveServer):**
   - 2FA가 비활성화된 상태: "2FA 활성화하기" 버튼 제공. 클릭 시 서버에서 임시 `TwoFactorSecret` 생성.
   - 브라우저에 `otpauth://` URI 생성: `otpauth://totp/Aristokeides:{Email}?secret={Secret}&issuer=Aristokeides`
   - CDN 기반 `qrcode.min.js`를 사용해 UI 상에 QR 코드를 그리고 비밀키 문자열도 텍스트로 보조 표시.
   - 사용자에게 OTP 6자리 입력을 요구하는 `<input>`과 검증 버튼 제공.
   - 사용자가 입력한 OTP 검증 성공 시, DB의 `IsTwoFactorEnabled`를 true로 전환, `TwoFactorSecret`을 저장하고, 생성된 10개의 복구 코드를 1회성 경고 박스로 제공하며 복사 권장.
3. **2FA 비활성화 기능:**
   - 2FA가 이미 활성화된 상태: "2FA 비활성화하기" 버튼 제공. 활성화 해제를 위해 현재 비밀번호 또는 현재 OTP 입력을 검증한 후 해제( IsTwoFactorEnabled = false, Secret = null로 초기화).
</action>
<acceptance_criteria>
- 설정 화면에 2FA 탭이 표시되며 UI 스타일이 기존 Layout 디자인과 조화를 이룬다.
- 2FA 활성화 과정에서 생성된 QR 코드가 정상 렌더링되고 OTP 앱에서 인식된다.
- 올바른 OTP 코드를 제출하면 2FA가 성공적으로 설정되며, 10개의 백업 복구 코드가 시각적으로 팝업 또는 박스 형태로 제공된다.
- 2FA 비활성화 시 정상적으로 비활성 모드로 복원된다.
</acceptance_criteria>
</task>

<task id="18A-4">
<title>로그인 컨트롤러 및 흐름 제어 (2FA 대응)</title>
<read_first>
- `Aristokeides.Api/Controllers/AuthController.cs` — `CookieLogin` 엔드포인트 참조 (L67-86)
- `Aristokeides.Api/Components/Pages/Login.razor` — 현재 로그인 form 구성 확인
</read_first>
<action>
1. **AuthController.cs `CookieLogin` 수정:**
   - 비밀번호 검증 성공 후, 해당 사용자가 `IsTwoFactorEnabled`인지 검사한다.
   - **활성화된 경우:** 정식 세션 쿠키를 바로 발급하지 않고, 단기 수명이 있는 "임시 2FA 세션" 쿠키(예: Claim `amr = 2fa_pending`, `userId = user.Id`)를 임시 발급하거나 상태 저장 후 Redirect to `/login-2fa`.
   - **비활성화된 경우:** 기존처럼 정식 쿠키를 발급하고 홈(`/`)으로 리다이렉트.

2. **Login2Fa.razor 페이지 신규 추가:**
   - `@page "/login-2fa"`
   - 사용자로부터 6자리 OTP 코드 또는 백업 복구 코드 입력을 받는 SSR 폼 페이지.
   - 임시 쿠키를 읽어 `userId`를 알아낸 후 `TwoFactorService`를 통해 입력값 검증.
   - 검증 성공 시: 임시 쿠키를 폐기하고 정식 로그인 세션 쿠키를 신규 발급한 뒤 홈(`/`)으로 리다이렉트.
   - 검증 실패 시: `/login-2fa?error=invalid_otp`로 리다이렉트하여 에러 표시.
</action>
<acceptance_criteria>
- 2FA 활성 사용자가 비밀번호 로그인 완료 시 즉시 `/login-2fa`로 강제 유도된다.
- `/login-2fa` 진입 시 임시 쿠키를 통해 안전하게 사용자 식별자가 확인된다.
- 올바른 OTP 또는 미사용 복구 코드 입력 시에만 정식 로그인 상태로 승격된다.
- 무효한 OTP 코드 입력 시 로그인되지 않고 에러 메시지가 정상 노출된다.
</acceptance_criteria>
</task>

## must_haves

- `Otp.Net` 라이브러리를 통해 표준 TOTP RFC 6238 규격을 준수해야 한다.
- 2FA 활성화 후 1회 노출되는 복구 코드(10개)가 제공되어야 한다.
- OTP QR 코드 렌더링은 이미지 빌더 서버 의존성 없이 클라이언트측 CDN QR 라이브러리(또는 순수 JS)로 구현해야 한다.
- 임시 2FA 보류 상태(2fa_pending)의 인증 쿠키는 실제 시스템 권한을 가지지 않아야 한다 (Middleware 등에서 일반 인증된 사용자에서 차단).

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Services/TwoFactorService.cs` | 신규 | TOTP 비밀키 생성 및 검증 핵심 서비스 |
| `Aristokeides.Api/Components/Pages/Login2Fa.razor` | 신규 | 2단계 인증 OTP 입력 페이지 (SSR 폼) |
| `Aristokeides.Api/Models/User.cs` | 수정 | 2FA 관련 데이터 모델 필드 추가 |
| `Aristokeides.Api/Data/AppDbContext.cs` | 수정 | User 모델 2FA 필드 정의 추가 |
| `Aristokeides.Api/Controllers/AuthController.cs` | 수정 | 2FA 2단계 로그인 인터셉트 및 검증 API 추가 |
| `Aristokeides.Api/Components/Pages/Settings.razor` | 수정 | Settings 내 2FA 설정 탭 및 UI 통합 |
