---
gsd_state_version: 1.0
milestone: v1.2
milestone_name: - Web UI Completion
status: completed
stopped_at: Phase 12 context gathered
last_updated: "2026-06-08T06:01:17.678Z"
progress:
  total_phases: 3
  completed_phases: 0
  total_plans: 1
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-08)

**Core value:** C# / .NET 기반의 뛰어난 성능을 바탕으로, Git 저장소 호스팅, 이슈 트래커, 코드 리뷰 등 협업에 필수적인 핵심 기능들을 가볍고 안정적으로 제공하는 것.
**Status:** Phase 11 complete
**Current focus:** Phase 12 — Repository Management UI
**Current Position:** Phase 11 executed. Ready for Phase 12.

## Last Session

**Stopped at:** Phase 12 context gathered
**Resume file:** .planning/phases/12-repository-management-ui/12-CONTEXT.md

## Decisions

- **D-01 (Phase 1):** Swashbuckle 7.3.2 사용 (10.x OpenApi v3 호환 문제)
- **D-02 (Phase 1):** User.Role은 string 타입 (Admin, Contributor, Reader)
- **D-03 (Phase 2):** Repository OwnerId changed from Guid to int to match User.Id
- **D-04 (Phase 2):** User.Username added to support Git paths like /{username}/{repo}.git
- **D-05 (Phase 3):** Use Blazor Server (SSR Mode) instead of SPA for straightforward Git data visualization.
- **D-06 (Phase 3):** Use highlight.js globally through CDN and inline scripts for lightweight 문법 강조(syntax highlighting).
- **D-07 (Phase 3):** Combined Auth policy scheme (JWT + Cookies) to unify backend API and UI views.
- **D-08 (Phase 4):** CreatorId uses DeleteBehavior.Restrict to prevent accidental user deletion with existing issues.
- **D-09 (Phase 4):** AssigneeId uses DeleteBehavior.SetNull so unassigning is safe when users are removed.
- **D-10 (Phase 4):** Composite unique index on (RepositoryId, LocalId) enforces per-repo issue numbering.

## Blockers

- 없음
