---
phase: 04-issue-management
plan: 01
subsystem: database
tags: [ef-core, blazor, kanban, issue-tracking, postgresql]

# Dependency graph
requires:
  - phase: 03-web-ui
    provides: Blazor SSR setup, AppDbContext, Repository/User models
provides:
  - BoardColumn and Issue EF Core models
  - Phase4IssueManagement database migration
  - Default board column seeding on repository creation
  - Blazor InteractiveServer mode enabled
affects: [04-issue-management]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Composite unique index on (RepositoryId, LocalId) for per-repo issue numbering"
    - "Default board column seeding in controller Create action"
    - "Restrict delete on creator FK, SetNull on assignee FK"

key-files:
  created:
    - Aristokeides.Api/Models/BoardColumn.cs
    - Aristokeides.Api/Models/Issue.cs
    - Aristokeides.Api/Migrations/20260530111050_Phase4IssueManagement.cs
  modified:
    - Aristokeides.Api/Models/User.cs
    - Aristokeides.Api/Models/Repository.cs
    - Aristokeides.Api/Data/AppDbContext.cs
    - Aristokeides.Api/Program.cs
    - Aristokeides.Api/Controllers/RepositoriesController.cs

key-decisions:
  - "D-08: CreatorId uses DeleteBehavior.Restrict to prevent accidental user deletion with existing issues"
  - "D-09: AssigneeId uses DeleteBehavior.SetNull so unassigning is safe when users are removed"
  - "D-10: Composite unique index on (RepositoryId, LocalId) enforces per-repo issue numbering"

patterns-established:
  - "BoardColumn seeding: 3 default columns (To Do, In Progress, Done) created with each new repo"
  - "Issue.LocalId scoped per repository for user-friendly issue numbering (#1, #2, etc.)"

requirements-completed: [ISSU-01, ISSU-02]

# Metrics
duration: 5min
completed: 2026-05-30
---

# Phase 4 Plan 1: Schema & Backend Infrastructure Summary

**BoardColumn and Issue EF Core models with migration, InteractiveServer enabled, and default kanban column seeding on repository creation**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-05-30T20:08:00+09:00
- **Completed:** 2026-05-30T20:12:00+09:00
- **Tasks:** 2
- **Files modified:** 8

## Accomplishments
- Created `BoardColumn` model (Id, RepositoryId, Name, Order) with full navigation properties
- Created `Issue` model (Id, LocalId, RepositoryId, Title, Description, CreatorId, AssigneeId, ColumnId, CreatedAt, UpdatedAt) with navigation properties to Repository, Creator, Assignee, Column
- Updated `User` and `Repository` models with navigation collections for issues and board columns
- Configured EF Core relationships: Cascade delete for repo→columns/issues, Restrict for creator, SetNull for assignee
- Added composite unique index on (RepositoryId, LocalId) for per-repo issue numbering
- Generated `Phase4IssueManagement` migration successfully
- Enabled Blazor InteractiveServer mode in Program.cs
- Seeded 3 default BoardColumns (To Do, In Progress, Done) on repository creation

## Task Commits

Each task was committed atomically:

1. **Task 1: model-and-db** - `59d7ec7` (feat: add BoardColumn and Issue models with EF Core migration)
2. **Task 2: update-program-and-seeding** - `76e92dd` (feat: enable InteractiveServer and seed default BoardColumns)

## Files Created/Modified
- `Aristokeides.Api/Models/BoardColumn.cs` - Kanban board column entity (Id, RepositoryId, Name, Order)
- `Aristokeides.Api/Models/Issue.cs` - Issue entity with full properties and navigation
- `Aristokeides.Api/Models/Repository.cs` - Added BoardColumns and Issues navigation collections
- `Aristokeides.Api/Models/User.cs` - Added CreatedIssues and AssignedIssues navigation collections
- `Aristokeides.Api/Data/AppDbContext.cs` - Added DbSets and full relationship configuration
- `Aristokeides.Api/Migrations/20260530111050_Phase4IssueManagement.cs` - Schema migration
- `Aristokeides.Api/Program.cs` - InteractiveServer components and render mode
- `Aristokeides.Api/Controllers/RepositoriesController.cs` - Default BoardColumn seeding

## Decisions Made
- **D-08:** Used `DeleteBehavior.Restrict` on CreatorId FK — prevents accidental user deletion when issues exist
- **D-09:** Used `DeleteBehavior.SetNull` on AssigneeId FK — allows user removal without cascading issue deletion
- **D-10:** Composite unique index on (RepositoryId, LocalId) — enforces unique sequential issue numbers per repo

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Self-Check: PASSED
- ✅ `BoardColumn.cs` exists with correct properties
- ✅ `Issue.cs` exists with correct properties
- ✅ `AppDbContext` has DbSets and configurations
- ✅ Migration file generated in Migrations folder
- ✅ `Program.cs` configures InteractiveServer
- ✅ `RepositoriesController.cs` seeds 3 default BoardColumns
- ✅ Build succeeds with 0 warnings, 0 errors

## Next Phase Readiness
- Schema and backend infrastructure complete, ready for Plan 02 (Issue CRUD API and Kanban UI)
- Database migration will auto-apply on startup via `MigrateAsync()`

---
*Phase: 04-issue-management*
*Completed: 2026-05-30*
