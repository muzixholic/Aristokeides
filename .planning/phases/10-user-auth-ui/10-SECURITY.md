---
phase: 10
slug: user-auth-ui
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-08
---

# Phase 10 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Client Web Browser → Web Application | 사용자가 웹 브라우저를 통해 회원가입/로그인 폼 데이터를 제출하고 세션 쿠키를 주고받음 | 평문 패스워드, 사용자명, 이메일, 세션 쿠키 (민감 정보 / HTTPS 보호 필수) |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-10-01 | Tampering | AuthController (cookie-register) | mitigate | [AuthController.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Controllers/AuthController.cs#L35-L58): 회원가입 API 바인딩 시 `Role` 파라미터를 배제하고 코드 내부에서 고정값 `"Reader"`를 할당하여 권한 상승 공격 차단 | closed |
| T-10-02 | Tampering | Database (User.PasswordHash) | mitigate | [AuthController.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Controllers/AuthController.cs#L55): 패스워드를 평문으로 저장하지 않고 BCrypt 해시 알고리즘을 사용해 암호화 저장 | closed |
| T-10-03 | Spoofing | Web Forms (CSRF) | mitigate | [Register.razor](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Components/Pages/Register.razor#L22) & [Login.razor](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Components/Pages/Login.razor#L22): Blazor SSR 정적 폼 제출 시 `<AntiforgeryToken />`을 강제 포함하고 미들웨어에서 유효성을 검증함 | closed |
| T-10-04 | Information Disclosure | Register/Login UI (User Enumeration) | accept | 사용자 경험(UX) 편의를 위해 이메일/사용자명 중복 사실을 화면에 명시적으로 고지함 (R-10-01 참조) | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| R-10-01 | T-10-04 | 이메일/사용자명 중복 확인 기능은 사용자 경험(UX) 증진을 위해 필수적이며, 일반적인 웹 서비스 정책에 준하여 가입 유무 고지 리스크를 수용함 | User & Agent | 2026-06-08 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-08 | 4 | 4 | 0 | Antigravity (Advanced Agentic Coding Assistant) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-08
