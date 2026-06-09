---
phase: 18
slug: security-auth-enhancements
status: verified
threats_open: 0
asvs_level: 2
created: 2026-06-09
---

# Phase 18 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Client Web Browser → Web Application | 사용자가 웹 브라우저를 통해 TOTP 및 OAuth2 데이터 전송, 세션 쿠키 수신 | 2FA OTP 입력값, 비밀번호, OAuth2 Access/ID Token, 세션 식별자 |
| External OAuth2 Provider (GitHub/Google) → Api Callback | OAuth2 콜백 엔드포인트를 통한 인증 증명 데이터 수신 | Authorization Code, State 파라미터, User Profile Claims |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-18-01 | Spoofing | OAuth2 Callback Handler | mitigate | [Program.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Program.cs#L64-L78): ASP.NET Core OAuth2 기본 State 파라미터 검증을 활성화하여 CSRF를 원천 차단함 | closed |
| T-18-02 | Tampering | OTP Verification | mitigate | [Program.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Program.cs#L125-L148): `amr = 2fa_pending` 클레임 기반 임시 세션에 대한 엄격한 자원 차단 미들웨어를 두어 OTP 무차별 시도 중에도 중요 리소스 접근 불가능하도록 처리함 | closed |
| T-18-03 | Information Disclosure | Two-Factor Setup UI | mitigate | [Settings.razor](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Components/Pages/Settings.razor): 2FA 활성화 설정 완료 후, UI 상에서 Secret Key와 QR 코드를 즉시 숨기고 영구적으로 다시 표시하지 않음 | closed |
| T-18-04 | Session Hijacking | Cookie Session | mitigate | [SessionValidationMiddleware.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Middleware/SessionValidationMiddleware.cs): 쿠키에 HttpOnly, Secure, SameSite=Lax 속성을 부여하고 매 요청마다 DB의 `UserSessions` 활성 유무(IsRevoked)를 검사하여 원격 로그아웃 즉시 접근을 거부함 | closed |
| T-18-05 | Privilege Escalation | Social Login Registration | mitigate | [AuthController.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Controllers/AuthController.cs#L193): 소셜 가입 콜백 핸들러 내부에서 가입 계정의 Role 권한을 `"Reader"` 고정값으로 강제 할당함 | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| (None) | — | — | — | — |

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-09 | 5 | 5 | 0 | Antigravity (Advanced Agentic Coding Assistant) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-09
