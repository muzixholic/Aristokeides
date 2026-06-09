---
phase: 19
slug: organization-teams
status: verified
threats_open: 0
asvs_level: 2
created: 2026-06-09
---

# Phase 19 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Client Web Browser → Organization Settings UI | 조직의 멤버 정보 변경, 팀 권한 관리, 조직 저장소 생성 폼 데이터 | 멤버 추가/삭제 API, 팀 권한 변경 매핑, 생성자 식별 세션 |
| Git Client → Git HTTP/SSH Endpoint | 조직 소유 비공개 저장소에 대한 Push/Pull 행위 시 권한 유효성 판별 | Git Credential(Basic Auth / Public Key), push/pull 명령 및 대상 리포지토리명 |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-19-01 | Elevation of Privilege | Organization Settings API | mitigate | [OrgSettings.razor](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Components/Pages/OrgSettings.razor): 조직 설정 관련 모든 화면과 멤버/팀 변경 행위에 대해 현재 사용자가 해당 조직의 `Owner` 역할인지 DB 상에서 유효성을 엄격하게 차단 및 검증함 | closed |
| T-19-02 | Information Disclosure | Repository Access Verification | mitigate | [GitSmartHttpMiddleware.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs) & [SshServerBackgroundService.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Services/Ssh/SshServerBackgroundService.cs): Git HTTP 미들웨어 및 SSH 서비스에서 리포지토리 탐색 시, 비공개 저장소는 사용자의 팀/개별 `Read` 이상 권한을 충족할 때만 조회를 허용하고 미달 시 403/Access Denied 처리함 | closed |
| T-19-03 | Elevation of Privilege | Git Push Operation | mitigate | [GitSmartHttpMiddleware.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs#L82) & [SshServerBackgroundService.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Services/Ssh/SshServerBackgroundService.cs#L273): Push 시점(`git-receive-pack`) 전에 조직원 및 소속 팀의 권한을 조회하여 `Write` 또는 `Admin` 등급 미만일 경우 쓰기 실행을 원천 차단함 | closed |
| T-19-04 | Tampering | Organization Creation | mitigate | [NewOrganization.razor](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Components/Pages/NewOrganization.razor#L45): 조직명 생성 시 정규식을 적용하여 포맷을 통제하고, 기존 사용자의 `Username` 및 기존 조직명과의 충돌 여부를 사전에 조회하여 URL 라우팅 침범을 방지함 | closed |
| T-19-05 | Tampering | Member Invitation | mitigate | [OrgSettings.razor](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Components/Pages/OrgSettings.razor#L95): 조직원 추가 시 실제 가입된 유효 사용자인지 확인하고, 이미 가입된 사용자의 중복 추가를 백엔드 고유 인덱스와 로직으로 예방함 | closed |

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
