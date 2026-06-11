---
phase: 25
slug: v1-5
status: draft
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-11
---

# Phase 25 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet test (xUnit) |
| **Config file** | none (Default xUnit) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~Aristokeides.Tests.Ssh"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~16 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~Aristokeides.Tests.Ssh"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 20 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 25-01-01 | 01 | 1 | v1.5 통합 검증 및 문서화 | — | N/A | manual | N/A | ✅ | ⬜ pending |
| 25-01-02 | 01 | 1 | v1.5 통합 검증 및 문서화 | — | N/A | manual | N/A | ✅ | ⬜ pending |
| 25-01-03 | 01 | 1 | v1.5 통합 검증 및 문서화 | — | N/A | integration | `dotnet test` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Aristokeides.Tests` 내 기존 테스트 셋이 모두 통과 상태여야 함

*If none: "Existing infrastructure covers all phase requirements."*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| TESTING.md 문서 생성 및 README.md 연동 | v1.5 통합 검증 및 문서화 | 문서 작업 | 루트에 TESTING.md 파일이 명확한 마크다운 양식으로 생성되었고 README.md에서 링크 참조가 동작하는지 확인 |
| RETROSPECTIVE.md 및 감사 문서 생성 | v1.5 통합 검증 및 문서화 | 문서 작업 | 회고록 및 마일스톤 감사 보고서가 정상적으로 기입되었는지 확인 |

*If none: "All phase behaviors have automated verification."*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 20s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
