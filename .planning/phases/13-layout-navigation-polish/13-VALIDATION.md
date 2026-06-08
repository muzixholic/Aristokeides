---
phase: 13
slug: layout-navigation-polish
status: approved
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-08
---

# Phase 13 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit + bUnit |
| **Config file** | none |
| **Quick run command** | `dotnet test E:\Workspace\VisualC#\Aristokeides\Aristokeides.Tests\Aristokeides.Tests.csproj --filter "LayoutAndStyleTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run quick command
- **After every plan wave:** Run full suite command
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 10 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 13-01-01 | 01 | 1 | app.css styling | — | N/A | integration | `dotnet test E:\Workspace\VisualC#\Aristokeides\Aristokeides.Tests\Aristokeides.Tests.csproj --filter "GlobalStylesheet_DefinesLayoutAndNavigationStyling"` | ✅ | ✅ green |
| 13-02-01 | 01 | 1 | MainLayout.razor layout updates | — | N/A | integration | `dotnet test E:\Workspace\VisualC#\Aristokeides\Aristokeides.Tests\Aristokeides.Tests.csproj --filter "MainLayout_RendersNavigationLinksAndFooter"` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|

*If none: "All phase behaviors have automated verification."*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 10s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-06-08

## Validation Audit 2026-06-08
| Metric | Count |
|--------|-------|
| Gaps found | 2 |
| Resolved | 2 |
| Escalated | 0 |
