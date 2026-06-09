---
phase: 19
slug: organization-teams
status: planning
threats_open: 5
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
| T-19-01 | Elevation of Privilege | Organization Settings API | mitigate | 모든 조직 설정 변경 API(멤버 관리, 팀 권한 관리)에서 현재 인증된 사용자가 해당 조직의 `Owner` 역할(OrganizationMember.Role == "Owner")을 가졌는지 검증 로직 강제 | open |
| T-19-02 | Information Disclosure | Repository Access Verification | mitigate | Git HTTP Middleware 및 SSH Command Bridge에서 리포지토리 조회 시, 조직 소유의 비공개(Private) 저장소는 사용자의 팀 소속 권한 또는 개별 권한이 `Read` 이상일 때만 조회를 허용하며 없으면 404/Access Denied 반환 | open |
| T-19-03 | Elevation of Privilege | Git Push Operation | mitigate | Push(쓰기) 명령인 `git-receive-pack` 실행 전에 사용자의 유효 권한을 조회하여 `Write` 또는 `Admin`이 아닌 경우 동작을 즉시 차단 | open |
| T-19-04 | Tampering | Organization Creation | mitigate | 조직 생성 시 영문/숫자/대시 기호만 허용하도록 정규식 검증을 실시하고, 기존의 일반 사용자명(Username)과 중복되는 명칭의 조직 생성을 차단하여 URL 라우팅 충돌 방지 | open |
| T-19-05 | Tampering | Member Invitation | mitigate | 멤버 추가 시 실제 데이터베이스에 가입되어 있는 사용자(User)만 추가할 수 있도록 사전에 ID 매핑 및 중복 추가 제약 조건 체크 적용 | open |

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
