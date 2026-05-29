# Walking Skeleton — Aristokeides

**Phase:** 1
**Generated:** 2026-05-29

## Capability Proven End-to-End

A signed-in user can successfully authenticate via API and receive a JWT token that grants access to a protected "Me" endpoint, verifying database reads and auth pipeline.

## Architectural Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Framework | ASP.NET Core 9.0 Web API | High performance, built-in DI, optimal for backend systems |
| Data layer | Entity Framework Core + PostgreSQL | Robust ORM, excellent PostgreSQL support, strongly typed |
| Auth | JWT Bearer Tokens | Stateless, API-first approach, easy to integrate with future frontends |
| DB Migrations | Auto-migration on startup | Simplicity for MVP, avoids manual CLI steps during early development |
| Directory layout | Standard ASP.NET Core layout | Follows .NET conventions for easier maintenance |

## Stack Touched in Phase 1

- [ ] Project scaffold (`dotnet new webapi`)
- [ ] Routing — `/api/auth/login`, `/api/auth/register`, `/api/users/me`
- [ ] Database — Create user, Read user
- [ ] UI — (No UI in this backend phase, but Swagger UI proves interactivity)
- [ ] Deployment — Documented local `dotnet run` command

## Out of Scope (Deferred to Later Slices)

- Email verification
- Password reset
- OAuth providers (GitHub/GitLab login)
- Frontend UI application
- Git repository hosting features

## Subsequent Slice Plan

Each later phase adds one vertical slice on top of this skeleton without altering its architectural decisions:

- Phase 2: Git Repository Hosting
- Phase 3: Issue Tracker & Kanban
- Phase 4: Code Review System
- Phase 5: CI/CD Pipeline
