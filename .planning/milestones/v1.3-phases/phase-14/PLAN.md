# Phase 14 Plan: 멀티 데이터베이스 지원 기반 마련

## 1. 🎯 Objective
- 단일 PostgreSQL 지원 구조에서 벗어나, `SQLite`, `PostgreSQL`, `MySQL/MariaDB` 중 하나를 선택해 사용할 수 있는 멀티 데이터베이스 지원 구조를 도입합니다.
- 향후 "설치 관리자(Setup Wizard)"에서 DB를 선택하고 동적으로 구성할 수 있도록 사전에 필요한 패키지 추가 및 `appsettings.json`, `Program.cs` 구조를 개편합니다.

## 2. 📝 Tasks

### Task 1: EF Core 멀티 Provider 패키지 설치
- `Aristokeides.Api.csproj`에 다음 패키지를 추가합니다:
  - `Microsoft.EntityFrameworkCore.Sqlite`
  - `Pomelo.EntityFrameworkCore.MySql`
- 기존 `Npgsql.EntityFrameworkCore.PostgreSQL` 패키지는 그대로 유지합니다.

### Task 2: `appsettings.json` 설정 구조 변경
- `appsettings.json`에 `Database` 섹션을 추가하고, 사용할 DB 종류 및 연결 문자열을 명시할 수 있도록 합니다.
  ```json
  "Database": {
    "Provider": "PostgreSQL", // 또는 "SQLite", "MySQL"
    "ConnectionString": "Host=localhost;Database=aristokeides;Username=postgres;Password=yourpassword"
  }
  ```
- 기존 `ConnectionStrings:DefaultConnection` 구조를 새로운 구조로 마이그레이션하거나, 호환성을 유지합니다.

### Task 3: `Program.cs` DbContext 동적 등록 구현
- `Program.cs`에서 `AddDbContext<AppDbContext>` 설정 시, `builder.Configuration["Database:Provider"]` 값을 읽어 `switch` 구문을 통해 분기합니다.
- 각 분기별로 다음을 호출합니다:
  - **PostgreSQL:** `options.UseNpgsql(...)`
  - **SQLite:** `options.UseSqlite(...)`
  - **MySQL:** `options.UseMySql(..., ServerVersion.AutoDetect(...))`
- 마이그레이션 파일들이 Provider별로 충돌하지 않도록, 각 Provider 호출 시 `.MigrationsAssembly("Aristokeides.Api")`를 명시하고, 모델 스냅샷과 마이그레이션 파일 관리를 위한 전략을 수립합니다 (예: 각 Provider별로 `Migrations/Postgres`, `Migrations/Sqlite`, `Migrations/Mysql` 디렉토리를 분리).

### Task 4: 기존 Migration 분리 및 SQLite/MySQL 초기 Migration 생성
- 기존 `Migrations` 폴더에 있는 파일들은 모두 PostgreSQL용이므로, `Migrations/Postgres`로 이동시키거나 네임스페이스를 조정합니다.
- SQLite와 MySQL에 대해 각각 초기 마이그레이션을 생성합니다. (명령어 예시: `dotnet ef migrations add InitialCreate --context AppDbContext --output-dir Migrations/Sqlite` - 단, 이때 `Program.cs`의 Provider를 임시로 SQLite로 설정하거나 DesignTimeDbContextFactory를 구성해야 함).

## 3. 🔍 Verification
- [ ] `appsettings.json`에서 `Provider`를 `SQLite`로 변경하고 실행했을 때 `sqlite.db` 파일이 정상적으로 생성되고 앱이 구동되는지 확인한다.
- [ ] `Provider`를 `PostgreSQL`로 변경하고 실행했을 때 기존처럼 Postgres DB에 연결되는지 확인한다.
- [ ] DB Provider 변경에 따라 자동 마이그레이션(`dbContext.Database.MigrateAsync()`)이 문제없이 수행되는지 확인한다.
