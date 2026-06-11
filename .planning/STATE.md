---
gsd_state_version: 1.0
milestone: v1.5
milestone_name: UI 테스트 자동화 및 SSH 호환성 개선
status: v1.5 active
stopped_at: Phase 24 planned
last_updated: "2026-06-11T03:42:46.446Z"
last_activity: 2026-06-11 — Phase 23 (Playwright E2E tests) completed
progress:
  total_phases: 4
  completed_phases: 0
  total_plans: 2
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-11)

**Core value:** C# / .NET 기반의 뛰어난 성능을 바탕으로, Git 저장소 호스팅, 이슈 트래커, 코드 리뷰 등 협업에 필수적인 핵심 기능들을 가볍고 안정적으로 제공하는 것.
**Status:** v1.5 active
**Current focus:** UI 테스트 자동화(bUnit & Playwright E2E) 및 SSH 서버의 현대적 호환성 개선
**Current Position:** Phase 24 planned, ready to execute

## Last Session

**Stopped at:** Phase 24 planned
**Resume file:** .planning/phases/24-ssh/24-PLAN.md

## Decisions

- D-01 (Phase 1): Swashbuckle 7.3.2 사용 (10.x OpenApi v3 호환 문제)
- D-02 (Phase 1): User.Role은 string 타입 (Admin, Contributor, Reader)
- D-03 (Phase 2): Repository OwnerId changed from Guid to int to match User.Id
- D-04 (Phase 2): User.Username added to support Git paths like /{username}/{repo}.git
- D-05 (Phase 3): Use Blazor Server (SSR Mode) instead of SPA for straightforward Git data visualization.
- D-06 (Phase 3): Use highlight.js globally through CDN and inline scripts for lightweight 문법 강조(syntax highlighting).
- D-07 (Phase 3): Combined Auth policy scheme (JWT + Cookies) to unify backend API and UI views.
- D-08 (Phase 4): CreatorId uses DeleteBehavior.Restrict to prevent accidental user deletion with existing issues.
- D-09 (Phase 4): AssigneeId uses DeleteBehavior.SetNull so unassigning is safe when users are removed.
- D-10 (Phase 4): Composite unique index on (RepositoryId, LocalId) enforces per-repo issue numbering.

## Blockers

- 없음

## Current Position

Phase: Phase 24 planned
Plan: 24-01 (SSH 서버 라이브러리 교체 + 호스트 키 하이브리드 + DB 감사 로깅)
Status: Planned — ready to execute
Last activity: 2026-06-11 — Phase 24 계획 수립 완료

## Operator Next Steps

- Execute Phase 24 using /gsd-execute-phase 24
