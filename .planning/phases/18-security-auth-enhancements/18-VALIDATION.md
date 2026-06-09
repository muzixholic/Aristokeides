---
phase: 18
slug: security-auth-enhancements
status: planning
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-09
---

# Phase 18 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET Core CLI) |
| **Config file** | [Aristokeides.Tests.csproj](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Tests/Aristokeides.Tests.csproj) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~SecurityAuthTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~1-5 seconds |

---

## Sampling Rate

- **After every task commit:** Run specific unit tests `dotnet test --filter "FullyQualifiedName~SecurityAuthTests"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 18A-T1 | 18A | 1 | 2FA TOTP | T-18-02, T-18-03 | TOTP Base32 Secret Key 생성 및 OTP 코드 유효성 검증 로직 검증 | unit | `dotnet test --filter "FullyQualifiedName~TwoFactorTests"` | ⬜ | ⬜ pending |
| 18A-T2 | 18A | 1 | 2FA TOTP | T-18-02 | User 2FA 활성화 설정 UI 및 QR 코드 표시 검증 | human-check | (manual) | ⬜ | ⬜ pending |
| 18A-T3 | 18A | 1 | 2FA 로그인 | T-18-02 | 2FA 활성 계정의 로그인 시 2FA 입력 페이지 리다이렉션 및 최종 세션 획득 검증 | unit/integration | `dotnet test --filter "FullyQualifiedName~TwoFactorLoginTests"` | ⬜ | ⬜ pending |
| 18B-T1 | 18B | 2 | OAuth2 | T-18-01, T-18-05 | Google/GitHub 콜백 인입 시 회원 정보 파싱, 신규 가입/연동 매핑 로직 검증 | unit/integration | `dotnet test --filter "FullyQualifiedName~OAuthTests"` | ⬜ | ⬜ pending |
| 18B-T2 | 18B | 2 | OAuth2 | — | 로그인 화면 내 소셜 로그인 링크 추가 및 콜백 리다이렉트 흐름 검증 | human-check | (manual) | ⬜ | ⬜ pending |
| 18C-T1 | 18C | 3 | 세션 관리 | T-18-04 | UserSession DB 기록 및 SessionValidationMiddleware 세션 무효화/로그아웃 검증 | unit/integration | `dotnet test --filter "FullyQualifiedName~SessionManagementTests"` | ⬜ | ⬜ pending |
| 18C-T2 | 18C | 3 | 세션 관리 | T-18-04 | 사용자 설정 페이지의 세션 목록 출력 및 개별 세션 강제 종료 UI 동작 검증 | human-check | (manual) | ⬜ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure needs to be verified. We will need to write the target unit test skeletons during each wave to verify the database and API behaviors.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| 2FA QR 코드 스캔 및 활성화 | 2FA TOTP | 모바일 OTP 앱과의 실제 QR 코드 스캔 및 동기화 동작 확인 필요 | `/settings` 보안 탭에서 2FA 활성화를 눌러 생성된 QR 코드를 휴대폰 OTP 앱으로 등록한 뒤, 브라우저에 표시된 6자리 코드를 입력하여 활성화 성공 및 복구 코드 발급 검증 |
| 소셜 로그인 동선 검증 | OAuth2 | Google 및 GitHub 외부 로그인 창으로의 정상 라우팅과 사용자 입력 흐름 확인 필요 | `/login` 페이지에서 소셜 버튼 클릭 시 소셜 공급자의 동의 화면으로 리다이렉트되고 올바른 콜백이 체이닝되는지 브라우저 흐름 검사 (실 가동 혹은 개발용 Mock Callback 활용) |
| 원격 기기 강제 로그아웃 | 세션 관리 | 브라우저 쿠키와 서버 DB 간의 실시간 미들웨어 연동 및 강제 Sign-Out 갱신 확인 | 두 개의 다른 브라우저(예: Chrome, Safari)에서 로그인한 후, Chrome의 설정에서 Safari 세션을 만료시키고, Safari 브라우저에서 페이지 새로고침 시 즉시 로그인 화면으로 튕겨 나가는지 확인 |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 5s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
