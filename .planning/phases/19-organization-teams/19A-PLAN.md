---
title: "조직 및 팀 데이터 모델 구축과 생성 UI 구현"
phase: 19
wave: 1
depends_on: []
files_modified:
  - Aristokeides.Api/Models/Organization.cs
  - Aristokeides.Api/Models/OrganizationMember.cs
  - Aristokeides.Api/Models/Team.cs
  - Aristokeides.Api/Models/TeamMember.cs
  - Aristokeides.Api/Models/RepositoryPermission.cs
  - Aristokeides.Api/Data/AppDbContext.cs
  - Aristokeides.Api/Components/Pages/NewOrganization.razor
autonomous: true
requirements:
  - "조직(Organization), 조직원(OrganizationMember), 팀(Team), 팀원(TeamMember), 저장소 권한(RepositoryPermission)의 데이터베이스 모델 및 제약 조건을 구축해야 한다."
  - "사용자는 웹 브라우저를 통해 새로운 조직을 생성할 수 있어야 하며, 조직 이름은 기존 사용자명이나 조직명과 겹치지 않는 고유값이어야 한다."
---

# Plan 19A: 조직 및 팀 데이터 모델 구축과 생성 UI 구현

## Objective

조직 및 팀, 권한 설정 기능을 수행하기 위한 핵심 DB 스키마(Organization, OrganizationMember, Team, TeamMember, RepositoryPermission)를 정의하고 이를 세 개의 데이터베이스 공급자(SQLite, Postgres, MySQL) 마이그레이션에 반영한다. 또한 사용자가 브라우저를 통해 고유한 이름을 검증하며 새로운 조직을 생성할 수 있는 UI(`NewOrganization.razor`)를 제공한다.

## Tasks

<task id="19A-1">
<title>조직 및 팀 데이터 모델 생성</title>
<read_first>
- `Aristokeides.Api/Models/User.cs` — 기존 엔터티 구조 참조
- `Aristokeides.Api/Data/AppDbContext.cs` — DB 설정 참조
</read_first>
<action>
1. `Aristokeides.Api/Models/Organization.cs` 모델 정의:
   ```csharp
   using System;
   using System.Collections.Generic;

   namespace Aristokeides.Api.Models;

   public class Organization
   {
       public int Id { get; set; }
       public required string Name { get; set; }
       public string? Description { get; set; }
       public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

       public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
       public ICollection<Team> Teams { get; set; } = new List<Team>();
       public ICollection<Repository> Repositories { get; set; } = new List<Repository>();
   }
   ```
2. `Aristokeides.Api/Models/OrganizationMember.cs` 모델 정의:
   ```csharp
   using System;

   namespace Aristokeides.Api.Models;

   public class OrganizationMember
   {
       public int Id { get; set; }
       public int OrganizationId { get; set; }
       public int UserId { get; set; }
       public required string Role { get; set; } = "Member"; // "Owner", "Member"
       public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

       public Organization Organization { get; set; } = null!;
       public User User { get; set; } = null!;
   }
   ```
3. `Aristokeides.Api/Models/Team.cs` 모델 정의:
   ```csharp
   using System.Collections.Generic;

   namespace Aristokeides.Api.Models;

   public class Team
   {
       public int Id { get; set; }
       public int OrganizationId { get; set; }
       public required string Name { get; set; }
       public string? Description { get; set; }

       public Organization Organization { get; set; } = null!;
       public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
       public ICollection<RepositoryPermission> Permissions { get; set; } = new List<RepositoryPermission>();
   }
   ```
4. `Aristokeides.Api/Models/TeamMember.cs` 모델 정의:
   ```csharp
   namespace Aristokeides.Api.Models;

   public class TeamMember
   {
       public int Id { get; set; }
       public int TeamId { get; set; }
       public int UserId { get; set; }

       public Team Team { get; set; } = null!;
       public User User { get; set; } = null!;
   }
   ```
5. `Aristokeides.Api/Models/RepositoryPermission.cs` 모델 정의:
   ```csharp
   using System;

   namespace Aristokeides.Api.Models;

   public class RepositoryPermission
   {
       public int Id { get; set; }
       public Guid RepositoryId { get; set; }
       public int? UserId { get; set; }
       public int? TeamId { get; set; }
       public required string AccessLevel { get; set; } // "Read", "Write", "Admin"

       public Repository Repository { get; set; } = null!;
       public User? User { get; set; }
       public Team? Team { get; set; }
   }
   ```
</action>
<acceptance_criteria>
- 5개의 신규 엔터티 파일이 `Models/` 폴더 하위에 정상 생성된다.
- 컴파일 에러 없이 빌드가 가능해야 한다.
</acceptance_criteria>
</task>

<task id="19A-2">
<title>AppDbContext 매핑 및 데이터베이스 마이그레이션 적용</title>
<read_first>
- `Aristokeides.Api/Data/AppDbContext.cs` — 기존 OnModelCreating 빌더 설정 구조 확인
</read_first>
<action>
1. `AppDbContext.cs`에 신규 DbSet 추가:
   - `DbSet<Organization>`
   - `DbSet<OrganizationMember>`
   - `DbSet<Team>`
   - `DbSet<TeamMember>`
   - `DbSet<RepositoryPermission>`
2. `OnModelCreating` 내에 각 테이블 매핑 정의:
   - Organization: Name에 유니크 인덱스 설정
   - OrganizationMember: `(OrganizationId, UserId)` 복합 유니크 인덱스 지정 및 외래키 구성
   - Team: `(OrganizationId, Name)` 복합 유니크 인덱스 지정 및 외래키 구성
   - TeamMember: `(TeamId, UserId)` 복합 유니크 인덱스 지정 및 외래키 구성
   - RepositoryPermission: 외래키 연동. `UserId`와 `TeamId`가 모두 null이거나 모두 채워지지 않도록 비즈니스 검증 지원 명시.
3. EF Core 마이그레이션(Sqlite, Postgres, Mysql 각각)을 추가하고 적용한다:
   - `dotnet ef migrations add AddOrganizationAndTeams --context SqliteAppDbContext -o Migrations/Sqlite`
   - `dotnet ef migrations add AddOrganizationAndTeams --context PostgresAppDbContext -o Migrations/Postgres`
   - `dotnet ef migrations add AddOrganizationAndTeams --context MysqlAppDbContext -o Migrations/Mysql`
</action>
<acceptance_criteria>
- 세 개의 데이터베이스 공급자용 마이그레이션 파일이 빌드 및 충돌 없이 성공적으로 생성된다.
- SQLite 개발용 로컬 디바이스에 데이터베이스 업데이트가 정상 반영된다.
</acceptance_criteria>
</task>

<task id="19A-3">
<title>조직 생성 UI (NewOrganization.razor) 구현</title>
<read_first>
- `Aristokeides.Api/Components/Pages/NewRepository.razor` — Blazor 페이지 구성 및 폼 바인딩 양식 참조
</read_first>
<action>
`Aristokeides.Api/Components/Pages/NewOrganization.razor` 파일을 신규 생성하여 조직을 생성할 수 있는 페이지를 구현한다.

1. **페이지 선언 및 인증 속성:**
   - `@page "/orgs/new"`
   - 사용자 인증 필요 (비인증자 접근 차단)
   - `@rendermode InteractiveServer`
2. **폼 필드 구성:**
   - 조직명 입력란 (`Name`): 영문 소문자, 숫자 및 대시(-) 기호만 사용 가능하도록 클라이언트 Regex 검사 및 소문자 자동 변경 제공.
   - 조직 설명 입력란 (`Description`): 텍스트 영역.
3. **서버 측 고유성 검증 및 생성 로직:**
   - 제출(Submit) 시, 입력된 이름이 기존 `Users.Username` 중 하나이거나 기존 `Organizations.Name` 중 하나와 일치하는지 체크. 일치 시 "이미 사용 중인 이름입니다." 오류 표시.
   - 검증 완료 시, DB에 신규 `Organization` 레코드를 생성하고, 현재 로그인한 사용자를 `Role = "Owner"`로 설정한 `OrganizationMember` 매핑 데이터를 함께 추가 및 트랜잭션 저장.
   - 저장 성공 후 생성된 조직 대시보드 주소(`/orgs/{orgname}`)로 리다이렉트. (없을 시 홈 `/`로 리다이렉트하도록 처리)
</action>
<acceptance_criteria>
- `/orgs/new` 페이지에서 조직명과 설명 폼이 정상 작동한다.
- 입력된 조직명이 기존 사용자명 또는 조직명과 겹치면 중복 에러가 화면에 동적으로 로드된다.
- 조직 생성 완료 시 생성한 사용자가 해당 조직의 `Owner` 자격으로 DB 조직원 목록에 적재된다.
</acceptance_criteria>
</task>

## must_haves

- 조직명(Name)은 영문 소문자, 숫자, 대시(-) 기호만 사용할 수 있는 검증 조건이 필수적으로 적용되어야 한다.
- 기존 사용자(User)의 Username과 충돌하는 조직(Organization)이 생성되는 것을 방지해야 한다.
- 조직 생성 완료 후 생성한 사용자는 자동으로 해당 조직의 `Owner` 권한 조직원으로 등록되어야 한다.

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Models/Organization.cs` | 신규 | 조직 엔터티 모델 |
| `Aristokeides.Api/Models/OrganizationMember.cs` | 신규 | 조직원 매핑 정보 모델 |
| `Aristokeides.Api/Models/Team.cs` | 신규 | 조직 내 팀 모델 |
| `Aristokeides.Api/Models/TeamMember.cs` | 신규 | 팀 멤버 매핑 모델 |
| `Aristokeides.Api/Models/RepositoryPermission.cs` | 신규 | 리포지토리별 팀/개인 권한 제어 모델 |
| `Aristokeides.Api/Data/AppDbContext.cs` | 수정 | DbSet 선언 및 OnModelCreating 제약 조건 정의 |
| `Aristokeides.Api/Components/Pages/NewOrganization.razor` | 신규 | 조직 신규 생성 Blazor 컴포넌트 |
