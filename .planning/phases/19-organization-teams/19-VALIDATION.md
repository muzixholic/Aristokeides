---
phase: 19
slug: organization-teams
status: verified
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-09
---

# Phase 19 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET Core CLI) |
| **Config file** | [Aristokeides.Tests.csproj](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Tests/Aristokeides.Tests.csproj) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~OrganizationTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~1-5 seconds |

---

## Sampling Rate

- **After every task commit:** Run specific unit tests `dotnet test --filter "FullyQualifiedName~OrganizationTests"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 19A-T1 | 19A | 1 | 조직 및 팀 모델 | T-19-04 | Organization, Team, Member, Permission 스키마 구성 및 생성 검증 | unit | `dotnet test --filter "FullyQualifiedName~OrganizationModelTests"` | ✅ | ✅ green |
| 19A-T2 | 19A | 1 | 조직 생성 UI | T-19-04 | `/orgs/new` 페이지 UI 및 중복 명칭 검증 테스트 | human-check | (manual) | ✅ | ✅ green |
| 19B-T1 | 19B | 2 | 저장소 소유권 개편 | — | 조직 소유 저장소 생성 및 물리 베어 저장소 경로 생성 검증 | unit/integration | `dotnet test --filter "FullyQualifiedName~OrgRepoCreationTests"` | ✅ | ✅ green |
| 19B-T2 | 19B | 2 | 권한 검증 개편 | T-19-02, T-19-03 | Git HTTP 및 SSH의 조직원/팀별 접근 권한 매트릭스(Read/Write/Admin) 판별 로직 검증 | unit/integration | `dotnet test --filter "FullyQualifiedName~GitPermissionTests"` | ✅ | ✅ green |
| 19C-T1 | 19C | 3 | 멤버/팀 관리 UI | T-19-01, T-19-05 | 조직 설정 화면 멤버 초청, 방출 및 팀 생성과 권한 할당 API 동작 검증 | unit/integration | `dotnet test --filter "FullyQualifiedName~OrgAdminApiTests"` | ✅ | ✅ green |
| 19C-T2 | 19C | 3 | 조직 홈 & 관리 화면 | T-19-01 | 조직 홈 대시보드 리스트 바인딩 및 관리 권한 차단 검증 | human-check | (manual) | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

기존의 사용자 인증 및 저장소 생성 유틸리티를 상속받아 사용하므로, 마이그레이션이 올바르게 생성되었는지와 CLI 테스트 구동 환경을 사전에 점검해야 합니다.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| 조직 및 사용자명 중복 생성 차단 | 조직 생성 UI | 웹 인터페이스 내에서 사용자명과 조직명 충돌 시 안내 메시지 렌더링 확인 | 가입된 사용자의 Username과 동일한 이름으로 조직 생성을 시도했을 때, 생성 폼에서 경고 메시지가 나타나며 차단되는지 확인 |
| 조직원 검색 및 자동완성 UX | 멤버/팀 관리 UI | 사용자 검색을 위한 AJAX/드롭다운 제어 및 실시간 렌더링 검사 | 조직 설정 멤버 추가 폼에 다른 사용자의 일부 이름이나 이메일을 쳤을 때, 검색 리스트가 화면에 노출되고 선택하여 초정할 수 있는지 검증 |
| HTTP/SSH Push 권한 거부 알림 | 권한 검증 개편 | 원격 터미널에서 Git push 시도 시 에러 메시지 텍스트 파싱 확인 | 권한이 없는 조직 저장소 주소로 `git push` 실행 시 터미널에서 `403 Forbidden` 또는 `Permission denied` 에러를 정확하게 뱉는지 테스트 |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 5s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-06-09
