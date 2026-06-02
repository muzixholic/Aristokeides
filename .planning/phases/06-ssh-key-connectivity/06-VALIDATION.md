---
phase: 6
slug: ssh-key-connectivity
status: draft
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-02
---

# Phase 6 — Validation Strategy

> SSH 키 관리 및 내장 SSH 서버의 연동성 피드백 루프를 수립하기 위한 테스트 검증 계약 문서입니다.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 |
| **Config file** | none — Wave 0 stubs will verify configuration |
| **Quick run command** | `dotnet test --filter Category=Unit` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~5 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter Category=Unit`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 06-01-01 | 01 | 1 | SSH-01, SSH-02 | T-06-01 / T-06-02 | 키 유효성(RSA 3072+) 및 Fingerprint 계산 검증 | Unit | `dotnet test --filter "FullyQualifiedName~SshKeyParserTests"` | ❌ W0 | ⬜ pending |
| 06-01-02 | 01 | 1 | SSH-01, SSH-02 | T-06-03 | 중복 방지 및 등록/삭제 API 및 UI 검증 | Unit/Integration | `dotnet test --filter "FullyQualifiedName~SshKeyRegistrationTests"` | ❌ W0 | ⬜ pending |
| 06-02-01 | 02 | 2 | SSH-03 | — | SSH Clone URL 정합성 검증 | Unit | `dotnet test --filter "FullyQualifiedName~RepositoryUrlTests"` | ❌ W0 | ⬜ pending |
| 06-02-02 | 02 | 2 | SSH-04, SSH-05 | T-06-04 / T-06-05 | ssh -T 진단 응답 및 FxSsh DB 연계 인증 검증 | Integration | `dotnet test --filter "FullyQualifiedName~SshTDiagnosticTests"` | ❌ W0 | ⬜ pending |
| 06-03-01 | 03 | 3 | SSH-06 | T-06-07 | 셸 차단 및 명령어 화이트리스트/경로 검증 | Integration | `dotnet test --filter "FullyQualifiedName~SshServerAuthTests"` | ❌ W0 | ⬜ pending |
| 06-03-02 | 03 | 3 | SSH-06 | T-06-06 / T-06-08 | OS Git 프로세스 양방향 파이핑 릴레이 검증 | Integration | `dotnet test --filter "FullyQualifiedName~SshCommandPipingTests"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Aristokeides.Tests/SshKeyParserTests.cs` — stubs for SSH-01, SSH-02
- [ ] `Aristokeides.Tests/SshKeyRegistrationTests.cs` — stubs for SSH-01, SSH-02
- [ ] `Aristokeides.Tests/RepositoryUrlTests.cs` — stubs for SSH-03
- [ ] `Aristokeides.Tests/SshTDiagnosticTests.cs` — stubs for SSH-04, SSH-05
- [ ] `Aristokeides.Tests/SshServerAuthTests.cs` — stubs for SSH-06
- [ ] `Aristokeides.Tests/SshCommandPipingTests.cs` — stubs for SSH-06

---

## Manual-Only Verifications

"All phase behaviors have automated verification."

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 5s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
