---
phase: 7
slug: 07-ssh-commit-signature
status: draft
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-04
---

# Phase 7 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 |
| **Config file** | Aristokeides.Tests/Aristokeides.Tests.csproj |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~SshCommitSignature"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~SshCommitSignature"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 07-01-01 | 01 | 1 | SSH-07 | T-07-01 | DB Migration 및 CommitSignature 엔티티가 정상 생성 및 연결되어야 함 | unit | `dotnet test --filter "SshCommitSignature"` | ✅ | ⬜ pending |
| 07-01-02 | 01 | 1 | SSH-07 | T-07-02 | SSH 서명 원시 데이터에서 서명용 공개키 및 SHA-256 지문을 정확하게 추출할 수 있어야 함 | unit | `dotnet test --filter "SshCommitSignature"` | ✅ | ⬜ pending |
| 07-01-03 | 01 | 1 | SSH-07 | T-07-03 | `ssh-keygen -Y verify`를 안전하게 래핑하여 SSH 커밋 디지털 서명 검증이 성공/실패 시 올바른 상태를 반환해야 함 | unit | `dotnet test --filter "SshCommitSignature"` | ✅ | ⬜ pending |
| 07-02-01 | 02 | 2 | SSH-07 | T-07-04 | SSH Push(`SshCommandBridge`) 및 HTTP Push(`GitSmartHttpMiddleware`) 완료 후 신규 커밋의 서명 검증 파이프라인이 정상 트리거되고 기록되어야 함 | integration | `dotnet test --filter "SshCommitSignature"` | ✅ | ⬜ pending |
| 07-03-01 | 03 | 3 | SSH-07 | T-07-05 | 웹 UI 커밋 히스토리 및 상세 페이지에 신뢰할 수 있는 커밋에 대해 "Verified" 배지가 정확하게 렌렌더링되어야 함 | integration | `dotnet test --filter "SshCommitSignature"` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Aristokeides.Tests/SshCommitSignatureTests.cs` — stubs for REQ-SSH-07

*If none: "Existing infrastructure covers all phase requirements."*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| 실제 Git 클라이언트(로컬 터미널)를 사용한 SSH/HTTP Push 동작 시 Verified 배지 표출 통합 UX 확인 | SSH-07 | 실제 웹 브라우저 UI상의 연두색 Verified 배지 렌더링 상태 시각적 검토 필요 | 1. SSH 서명이 적용된 커밋을 Push합니다.<br>2. 웹 UI 커밋 목록 및 상세 페이지에 Verified 배지가 예쁘게 표시되는지 브라우저에서 직접 육안 확인합니다. |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 15s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
