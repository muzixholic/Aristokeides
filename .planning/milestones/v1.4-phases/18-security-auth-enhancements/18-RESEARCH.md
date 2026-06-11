# Phase 18: 보안 및 인증 기능 강화 — 리서치

## 1. 2단계 인증 (2FA - TOTP)

### 1.1. 주요 설계 및 워크플로우

1. **DB 스키마 추가 (User 모델):**
   - `bool IsTwoFactorEnabled { get; set; }`
   - `string? TwoFactorSecret { get; set; }`
   - `string? TwoFactorRecoveryCodes { get; set; }` (쉼표나 세미콜론으로 구분된 해시 복구 코드)

2. **TOTP 라이브러리:**
   - C#에서 TOTP를 검증하기 위해 `Otp.NET` 패키지(Base32 디코딩 및 TOTP 계산 지원)를 추가합니다.
   - `dotnet add package Otp.NET`

3. **QR 코드 생성:**
   - 서버 측에서 이미지 생성 라이브러리(SkiaSharp 등)를 추가하면 OS 종속성(Docker 호환성 등) 문제가 발생할 수 있습니다.
   - **대안:** 클라이언트 측에서 JS QR Code 라이브러리(CDN 제공 `qrcode.min.js`)를 사용하여 브라우저에서 `otpauth://` URI를 기반으로 즉석 렌더링합니다.
   - URI 형식: `otpauth://totp/Aristokeides:{Email}?secret={Secret}&issuer=Aristokeides`

4. **2FA 활성화 흐름:**
   - 사용자가 설정에서 "2FA 활성화" 요청 -> 32자 랜덤 Base32 Secret Key 생성.
   - 비밀키와 QR 코드를 화면에 표시.
   - 사용자가 OTP 코드를 입력하여 검증 완료 시, DB에 `IsTwoFactorEnabled = true`, `TwoFactorSecret` 및 10개의 복구 코드(Recovery Codes) 저장.

5. **2FA 로그인 흐름:**
   - 사용자가 이메일/비밀번호로 로그인 시도.
   - 비밀번호가 맞을 때, 해당 사용자의 `IsTwoFactorEnabled`가 `true`인지 확인.
   - `true`라면 일반 로그인 완료 쿠키를 발급하지 않고, 임시 2FA 인증 대기 클레임(예: `Amr = 2fa_pending`, `UserId = user.Id`)을 담은 쿠키 또는 메모리 상태를 저장 후 `/login-2fa` 페이지로 리다이렉트.
   - `/login-2fa`에서 사용자가 OTP 또는 복구 코드를 입력하여 검증 성공 시, 최종 로그인 처리(일반 로그인 쿠키 발급).

---

## 2. OAuth2 소셜 로그인 (GitHub & Google)

### 2.1. 라이브러리 및 패키지
- ASP.NET Core 공식 및 커뮤니티 패키지를 사용합니다:
  - `Microsoft.AspNetCore.Authentication.Google` (Google 로그인)
  - `AspNet.Security.OAuth.GitHub` (GitHub 로그인)

### 2.2. DB 스키마 (UserSocialLogin)
다중 소셜 계정 매핑을 위해 `UserSocialLogin` 테이블을 설계합니다:
- `int Id` (PK)
- `int UserId` (FK to Users)
- `string Provider` (예: "GitHub", "Google")
- `string ProviderKey` (외부 서비스의 고유 사용자 ID)
- `DateTime CreatedAt`

### 2.3. 인증 흐름
1. 사용자가 웹 UI에서 "GitHub/Google 로그인" 클릭 -> `/api/auth/external-login?provider=GitHub` 호출.
2. API에서 `Challenge`를 반환하여 소셜 로그인 공급자 페이지로 리다이렉트.
3. 소셜 로그인 완료 후 콜백 주소(`/api/auth/external-login-callback`)로 리턴.
4. 콜백 핸들러에서 외부 사용자 정보(Email, ProviderKey) 추출.
5. `UserSocialLogin` 테이블에서 일치하는 `ProviderKey`를 찾거나, 해당 Email로 가입된 사용자 조회:
   - 이미 연결된 계정이 있으면 -> 바로 로그인 쿠키 발급.
   - 연결된 계정이 없지만 동일 이메일 사용자가 있으면 -> 해당 사용자 계정에 소셜 연동 정보를 추가하고 로그인 쿠키 발급.
   - 이메일도 없으면 -> 자동으로 신규 가입 프로세스 진행 (Role="Reader", 임의의 사용자명 생성) 후 연동 정보 저장 및 로그인.

---

## 3. 세션 관리 및 보안 정책 (Active Sessions & Remote Logout)

### 3.1. DB 스키마 (UserSession)
활성 세션을 DB에서 관리하여 리스트 조회 및 원격 무효화를 구현합니다:
- `string Id` (PK, 암호학적으로 안전한 무작위 토큰 또는 Guid)
- `int UserId` (FK to Users)
- `string? UserAgent` (접속 브라우저 및 기기 판별용)
- `string? IpAddress` (접속 IP)
- `DateTime CreatedAt`
- `DateTime LastActiveAt`
- `bool IsRevoked`

### 3.2. 세션 검증 미들웨어 (SessionValidationMiddleware)
1. 로그인(Cookie 로그인, OAuth2 로그인, API JWT 발급) 시 DB에 `UserSession` 레코드를 생성하고, 세션 토큰(`SessionId`)을 클레임에 추가합니다.
2. HTTP 요청이 들어올 때마다 클레임의 `SessionId`가 DB에서 유효하며 `IsRevoked == false` 상태인지 체크하는 커스텀 미들웨어를 실행합니다.
3. 세션이 무효화되었거나 DB에 없으면 `HttpContext.SignOutAsync("Cookies")`를 호출하여 쿠키를 삭제하고 `/login`으로 튕겨냅니다.
4. 성능을 위해 `LastActiveAt` 업데이트는 매 요청마다 하지 않고, 5분 주기로 스로틀링합니다.

---

## 4. 보안 아키텍처 및 위협 모델링

| 위협 (Threat) | 완화책 (Mitigation) |
|---|---|
| **비밀번호 유출** | TOTP 2FA를 활성화하여 비밀번호가 노출되더라도 OTP 기기가 없으면 로그인 차단. |
| **인라인 QR 코드 악용** | OTP 등록용 QR 코드는 2FA 인증이 완료되기 전 임시 세션에서만 렌더링되도록 차단. |
| **세션 탈취 (Session Hijacking)** | 기기 도난 및 공용 PC 방치 시, 원격 로그아웃을 통해 활성 세션을 삭제하여 강제 로그아웃 유도. |
| **OAuth2 CSRF 공격** | ASP.NET Core Authentication 스택의 State 파라미터 유효성 검증을 기본 적용하여 위조 방지. |
