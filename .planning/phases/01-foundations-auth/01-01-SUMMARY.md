---
phase: 1
plan: 01
subsystem: api-foundation
tags: [dotnet, efcore, jwt, auth, swagger]
requires: []
provides: [auth-api, user-model, db-context, jwt-pipeline]
affects: [all-future-phases]
tech-stack:
  added:
    - ASP.NET Core 10.0
    - Entity Framework Core 10.0.8
    - Npgsql (PostgreSQL provider)
    - BCrypt.Net-Next 4.2.0
    - Swashbuckle.AspNetCore 7.3.2
    - Microsoft.AspNetCore.Authentication.JwtBearer
  patterns:
    - Minimal API Program.cs
    - Auto-migration on startup
    - JWT Claims-based authorization
key-files:
  created:
    - Aristokeides.slnx
    - Aristokeides.Api/Program.cs
    - Aristokeides.Api/Models/User.cs
    - Aristokeides.Api/Data/AppDbContext.cs
    - Aristokeides.Api/Controllers/AuthController.cs
    - Aristokeides.Api/Controllers/UsersController.cs
    - Aristokeides.Api/Migrations/20260529050118_InitialCreate.cs
    - dotnet-tools.json
    - .gitignore
  modified:
    - Aristokeides.Api/appsettings.json
    - Aristokeides.Api/Aristokeides.Api.csproj
key-decisions:
  - "Swashbuckle 7.3.2 사용 (10.x는 .NET 10 SDK와 OpenApi v3 호환 문제로 다운그레이드)"
  - "User 모델에 Email 유니크 인덱스 추가 (DB 레벨 중복 방지)"
  - "Role은 string으로 저장 (Admin, Contributor, Reader — enum 대신 유연성 확보)"
requirements-completed:
  - AUTH-01
  - AUTH-02
duration: "8 min"
completed: "2026-05-29"
---

# Phase 1 Plan 01: Walking Skeleton & Auth API Summary

ASP.NET Core 10.0 Web API 스켈레톤 구축, EF Core + PostgreSQL 데이터베이스 설정 및 자동 마이그레이션, JWT 기반 인증/인가 파이프라인 구현.

## Execution Details

- **Duration:** ~8 min
- **Tasks:** 4/4 complete
- **Files created:** 9
- **Files modified:** 2

## What Was Built

1. **프로젝트 스켈레톤**: `Aristokeides.slnx` 솔루션에 `Aristokeides.Api` (Web API)와 `Aristokeides.Tests` (xUnit) 프로젝트 구성
2. **데이터베이스**: EF Core + PostgreSQL (`Npgsql`) — `User` 엔터티, Email 유니크 인덱스, 앱 시작 시 `MigrateAsync()` 자동 실행
3. **인증 API**: 
   - `POST /api/auth/register` — BCrypt 비밀번호 해시, 기본 역할 "Reader" 할당
   - `POST /api/auth/login` — JWT 토큰 발급 (Role claim 포함)
   - `GET /api/users/me` — `[Authorize]` 보호, JWT Claims에서 사용자 정보 추출
4. **Swagger UI**: 개발 환경에서 JWT Bearer 토큰 인증 지원

## Deviations from Plan

- **[Rule 1 - Bug Fix] Swashbuckle 버전 다운그레이드** — Swashbuckle.AspNetCore 10.1.7이 Microsoft.OpenApi v3를 사용하여 `Microsoft.OpenApi.Models` 네임스페이스가 존재하지 않는 호환성 문제 발생. 7.3.2로 다운그레이드하여 해결.
- **[Rule 1 - Bug Fix] EF Core 버전 충돌 해결** — Tests 프로젝트에서 EF Core 10.0.4 vs 10.0.8 버전 충돌 경고. Tests 프로젝트에 EF Core + Relational 10.0.8 명시적 추가로 해결.
- **[Rule 1 - Bug Fix] .gitignore 누락** — 초기 커밋에 bin/obj 폴더가 포함됨. dotnet gitignore 템플릿 추가 후 amend로 수정.

**Total deviations:** 3 auto-fixed (2 패키지 호환성, 1 빌드 아티팩트). **Impact:** 없음 — 모두 개발 환경 설정 이슈로 기능에 영향 없음.

## Self-Check: PASSED

- [x] `Aristokeides.slnx` 존재
- [x] `dotnet build` — 0 warnings, 0 errors
- [x] `dotnet test` — 1/1 통과
- [x] `AppDbContext.cs` 존재, `User` DbSet 등록
- [x] `Program.cs`에 `MigrateAsync()` 포함
- [x] `AuthController.cs`에 Register/Login 포함
- [x] `UsersController.cs`에 `[Authorize]` Me 엔드포인트
- [x] JWT 인증 파이프라인 구성 완료
- [x] Swagger UI + JWT Bearer 지원
- [x] `Migrations/InitialCreate` 생성됨
- [x] git log에 01-01 관련 커밋 4개 존재

## Next Phase Readiness

Phase complete, ready for next step.
