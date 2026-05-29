---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: completed
last_updated: "2026-05-29T06:58:18.034Z"
progress:
  total_phases: 4
  completed_phases: 2
  total_plans: 2
  completed_plans: 2
  percent: 50
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-05-29)

**Core value:** C# / .NET 기반의 뛰어난 성능을 바탕으로, Git 저장소 호스팅, 이슈 트래커, 코드 리뷰 등 협업에 필수적인 핵심 기능들을 가볍고 안정적으로 제공하는 것.
**Status:** Phase 3 complete
**Current focus:** Phase 4: Issue Management
**Current Position:** Pending plan phase

## Last Session

**Stopped at:** Phase 4 context gathered
**Resume file:** .planning/phases/04-issue-management/04-CONTEXT.md

## Decisions

- **D-01 (Phase 1):** Swashbuckle 7.3.2 사용 (10.x OpenApi v3 호환 문제)
- **D-02 (Phase 1):** User.Role은 string 타입 (Admin, Contributor, Reader)
- **D-03 (Phase 2):** Repository OwnerId changed from Guid to int to match User.Id
- **D-04 (Phase 2):** User.Username added to support Git paths like /{username}/{repo}.git
- **D-05 (Phase 3):** Use Blazor Server (SSR Mode) instead of SPA for straightforward Git data visualization.
- **D-06 (Phase 3):** Use highlight.js globally through CDN and inline scripts for lightweight code block styling.
- **D-07 (Phase 3):** Combined Auth policy scheme (JWT + Cookies) to unify backend API and UI views.

## Blockers

- None
