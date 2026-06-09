---
phase: 18
slug: security-auth-enhancements
status: planning
threats_open: 5
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
| T-18-01 | Spoofing | OAuth2 Callback Handler | mitigate | ASP.NET Core 기본 State 검증 및 CSRF 방지 장치 활성화 | open |
| T-18-02 | Tampering | OTP Verification | mitigate | 연속 5회 OTP 검증 실패 시 세션 잠금 또는 2FA 시도 제한 적용 | open |
| T-18-03 | Information Disclosure | Two-Factor Setup UI | mitigate | 활성화 성공 후에는 2FA Secret Key를 화면에 다시 표시하지 않고 영구 은폐 | open |
| T-18-04 | Session Hijacking | Cookie Session | mitigate | HttpOnly, Secure(HTTPS 전용), SameSite=Lax 쿠키 속성 적용 및 서버 측 DB 검증 미들웨어를 통한 실시간 무효화 처리 | open |
| T-18-05 | Privilege Escalation | Social Login Registration | mitigate | 소셜 계정 최초 가입 시에도 임의의 권한 조작을 차단하고 Role을 `"Reader"`로 강제 고정 | open |

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
| 2026-06-09 | 5 | 0 | 5 | Antigravity (Advanced Agentic Coding Assistant) |

---

## Sign-Off

- [ ] All threats have a disposition (mitigate / accept / transfer)
- [ ] Accepted risks documented in Accepted Risks Log
- [ ] `threats_open: 0` confirmed
- [ ] `status: verified` set in frontmatter

**Approval:** pending
