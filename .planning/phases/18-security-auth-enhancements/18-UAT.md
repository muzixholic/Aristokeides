---
status: planning
phase: 18-security-auth-enhancements
source:
  - .planning/phases/18-security-auth-enhancements/18A-PLAN.md
  - .planning/phases/18-security-auth-enhancements/18B-PLAN.md
  - .planning/phases/18-security-auth-enhancements/18C-PLAN.md
started: "2026-06-09T21:38:00Z"
updated: "2026-06-09T21:38:00Z"
---

## Current Test

[planning phase]

## Tests

### 1. 2FA (TOTP) 활성화 및 검증 테스트
expected: |
  로그인한 사용자가 설정 화면에서 2FA 활성화를 클릭했을 때, 랜덤 Base32 Secret Key와 함께 브라우저에서 QR 코드가 렌더링되어야 한다. 사용자가 인증 앱(Google Authenticator 등)으로 QR 코드를 스캔한 뒤 생성된 OTP 6자리 번호를 입력하고 검증을 완료하면, DB의 IsTwoFactorEnabled 필드가 true가 되고 화면에 백업 복구 코드 목록이 표시되어야 한다.
result: pending

### 2. 2FA 로그인 차단 및 성공 흐름 테스트
expected: |
  2FA가 활성화된 계정으로 로그인 시도 시, 비밀번호가 일치하더라도 메인 화면으로 진입하지 않고 `/login-2fa` 화면으로 리다이렉트되어야 한다. OTP 코드를 올바르게 입력하면 최종 로그인(쿠키 세션 획득)이 되어 홈/대시보드로 진입해야 하며, 잘못된 OTP 코드를 연속 입력하면 에러 메시지가 표시되어야 한다.
result: pending

### 3. OAuth2 소셜 로그인 연동 및 회원가입 테스트
expected: |
  홈페이지 또는 로그인 화면에서 'GitHub 로그인' 또는 'Google 로그인' 버튼을 클릭하면 외부 제공자 인증 페이지로 이동해야 한다. 인증 완료 후 성공적으로 콜백을 받으면, 신규 사용자의 경우 Reader 권한으로 자동 회원가입 처리된 뒤 자동 로그인되어 대시보드로 이동해야 하고, 기존 동일 이메일 사용자는 소셜 계정이 자동 연동되어 로그인되어야 한다.
result: pending

### 4. 세션 목록 조회 테스트
expected: |
  사용자 설정 페이지의 '보안/세션 관리' 탭에서 현재 자신이 로그인한 브라우저의 User-Agent 정보와 IP 주소, 그리고 활성 상태 여부(현재 세션 표시 포함)가 목록으로 올바르게 로딩되어야 한다.
result: pending

### 5. 원격 세션 무효화 및 강제 로그아웃 테스트
expected: |
  세션 목록에서 다른 기기/브라우저의 세션에 대해 '세션 종료' 버튼을 클릭하면 해당 세션의 DB IsRevoked 플래그가 true로 변경되어야 한다. 해당 기기/브라우저에서 임의의 요청을 보낼 때 SessionValidationMiddleware에 의해 세션이 무효화되었음이 감지되어 세션 쿠키가 삭제되고 자동으로 로그인 화면(/login)으로 강제 리다이렉트되어야 한다.
result: pending

## Summary

total: 5
passed: 0
issues: 0
pending: 5
skipped: 0

## Gaps

[none yet]
