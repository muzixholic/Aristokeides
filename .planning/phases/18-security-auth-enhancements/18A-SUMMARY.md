# Phase 18 Wave 1: 2FA TOTP 코어 로직 및 활성화/로그인 연동 작업 요약

## 1. 작업 개요

- **목표:** 계정 보안 강화를 위해 TOTP 기반 2단계 인증(2FA)을 도입하고, 사용자 프로필 설정에서의 활성화/비활성화 기능 및 로그인 프로세스(2FA 챌린지) 연동을 완료한다.
- **수행일자:** 2026년 6월 9일
- **상태:** 성공적으로 완료 및 모든 테스트 검증 통과

---

## 2. 세부 구현 내용

### 1) 패키지 추가 및 데이터 모델 확장
- `Otp.Net` 라이브러리를 API 프로젝트에 추가하여 TOTP RFC 6238 표준 코어 기능을 획득했습니다.
- [User.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/User.cs) 모델에 2FA 상태와 비밀키, 백업 복구 코드를 저장할 속성들을 추가하고 [AppDbContext.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Data/AppDbContext.cs)에 컬럼 매핑 설정을 구성했습니다.
- SQLite, PostgreSQL, MySQL에 대응하는 EF Core 마이그레이션(`AddUserTwoFactorColumns`)을 각각 생성하고 SQLite 로컬 개발 데이터베이스에 우선 반영을 완료했습니다.

### 2) TwoFactorService 구현
- [TwoFactorService.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Services/TwoFactorService.cs)를 신규 작성하여 다음 핵심 비즈니스 로직을 제공합니다.
  - 임의의 20바이트 보안 키 생성 및 Base32 변환
  - 현재 시간 스텝 기준으로 전후 30초 오차 범위(VerificationWindow)를 감안한 TOTP 검증
  - 10자리의 영문 대문자/숫자로 구성된 10개의 백업 복구 코드 생성 및 1회성 사용 검증/소진 로직
- [Program.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Program.cs)에 `TwoFactorService`를 Scoped 의존성으로 등록했습니다.

### 3) 사용자 설정 2FA UI 연동
- [Settings.razor](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Components/Pages/Settings.razor)에 "보안 및 2FA" 탭을 추가하고 한국어로 UI를 통합했습니다.
- [App.razor](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Components/App.razor)에 `qrcode.min.js` CDN을 포함하고 JS Interop 헬퍼를 추가하여 이미지 생성 서버 없이 순수 클라이언트 측 QR 코드가 그려지도록 구현했습니다.
- 2FA 미활성 상태일 때 활성화를 유도하고, 활성화 성공 시 10개의 복구 코드를 1회성 경고 박스로 화면에 노출하여 복사를 권장합니다.
- 2FA 활성 상태일 때 계정 비밀번호 입력을 통해 2FA를 안전하게 해제(비활성화)할 수 있도록 연동했습니다.

### 4) 로그인 흐름 제어 (2FA 챌린지)
- [AuthController.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Controllers/AuthController.cs) 내 `CookieLogin`을 확장하여 2FA 활성 사용자는 임시 2FA 인증 상태 쿠키(`amr = 2fa_pending`)를 부여받고 즉시 `/login-2fa`로 리다이렉트하도록 했습니다.
- 신규 페이지 [Login2Fa.razor](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Components/Pages/Login2Fa.razor)를 생성하여 6자리 OTP 또는 복구 코드를 입력받고, `/api/auth/2fa/verify` 엔드포인트를 통해 검증 후 정식 로그인 세션으로 승격 처리되도록 했습니다.
- [Program.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Program.cs)에 인라인 미들웨어를 배치하여 `amr == 2fa_pending` 클레임을 가진 사용자가 `/login-2fa`나 로그인/로그아웃 관련 필수 API, 정적 에셋 등을 제외한 다른 리소스에 임의로 접근하는 행위를 전면 차단했습니다.

---

## 3. 테스트 및 검증 결과

- **신규 테스트 클래스 생성:** [TwoFactorTests.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Tests/TwoFactorTests.cs)
- **테스트 커버리지 항목:**
  1. Base32 시크릿 키 생성 유효성 검증
  2. 현재 시간 기준 올바른 TOTP 코드 검증 성공 및 잘못된 코드 실패 검증
  3. 10개의 백업 복구 코드 생성 및 단일 코드 소비 후 소진 처리와 중복 소비 차단 검증
  4. 2FA 비활성 사용자의 정상 로그인 리다이렉트 검증
  5. 2FA 활성 사용자의 로그인 시도 시 `2fa_pending` 쿠키 발급 및 `/login-2fa` 리다이렉션 검증
  6. 임시 세션 상태에서 올바른 TOTP 입력을 통한 최종 세션 획득 검증
  7. 임시 세션 상태에서 백업 복구 코드 입력을 통한 최종 로그인 및 DB 내 사용된 복구 코드 차감 검증
- **테스트 구동 결과:** 전체 58개 테스트 케이스 중 **58개 전원 통과 (Passed)**

---

## 4. 산출물 목록

| 파일 경로 | 작업 구분 | 설명 |
|---|---|---|
| [TwoFactorService.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Services/TwoFactorService.cs) | 신규 | Base32 키 생성, TOTP 검증, 복구 코드 처리 |
| [Login2Fa.razor](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Components/Pages/Login2Fa.razor) | 신규 | 2차 인증 코드 입력 전용 Blazor 페이지 |
| [TwoFactorTests.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Tests/TwoFactorTests.cs) | 신규 | 2FA 코어 및 컨트롤러/로그인 통합 단위 테스트 |
| [User.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/User.cs) | 수정 | 2FA 설정 여부, 시크릿 키, 복구 코드 데이터 모델 확장 |
| [AppDbContext.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Data/AppDbContext.cs) | 수정 | 2FA 컬럼 속성 및 매핑 정보 추가 |
| [AuthController.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Controllers/AuthController.cs) | 수정 | CookieLogin 챌린지 및 2FA 설정/검증 엔드포인트 추가 |
| [Settings.razor](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Components/Pages/Settings.razor) | 수정 | 보안 및 2FA 탭 및 QR 코드 등록 양식 화면 통합 |
| [App.razor](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Components/App.razor) | 수정 | qrcode.min.js CDN 로딩 및 QR 드로잉 헬퍼 추가 |
| [Program.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Program.cs) | 수정 | TwoFactorService 등록 및 2FA pending 차단 미들웨어 추가 |
| [18A-SUMMARY.md](file:///Users/muzixholic/Projects/Aristokeides/.planning/phases/18-security-auth-enhancements/18A-SUMMARY.md) | 신규 | Phase 18 Wave 1 요약 보고서 (본 파일) |
