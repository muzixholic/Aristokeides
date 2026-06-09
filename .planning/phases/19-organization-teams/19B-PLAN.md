---
title: "저장소 소유권 개편 및 Git HTTP/SSH 권한 검증 고도화"
phase: 19
wave: 2
depends_on: [19A-PLAN.md]
files_modified:
  - Aristokeides.Api/Models/Repository.cs
  - Aristokeides.Api/Data/AppDbContext.cs
  - Aristokeides.Api/Components/Pages/NewRepository.razor
  - Aristokeides.Api/Services/RepositoryCreationBackgroundWorker.cs
  - Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs
  - Aristokeides.Api/Services/Ssh/SshServerBackgroundService.cs
autonomous: true
requirements:
  - "저장소는 일반 사용자뿐만 아니라 조직이 소유할 수 있도록 소유 구조를 유연화하고 DB 모델을 개편해야 한다."
  - "Git HTTP 및 SSH 접속 파이프라인에서 조직 저장소에 대한 접근 시 사용자의 개별 및 소속 팀별 권한 등급을 식별하여 적절히 허용/거부 처리를 적용해야 한다."
---

# Plan 19B: 저장소 소유권 개편 및 Git HTTP/SSH 권한 검증 고도화

## Objective

`Repository` 모델의 소유 관계를 사용자(`OwnerId`) 혹은 조직(`OrganizationId`) 둘 중 하나가 되도록 확장하고 관련 스키마 마이그레이션을 생성한다. 저장소 생성 UI를 확장하여 생성할 소유자(사용자 자신 혹은 관리 중인 조직)를 정할 수 있도록 한다. 백엔드 Git HTTP 미들웨어 및 SSH 인증 백그라운드 서비스에서 조직 소유 저장소 접근 시 권한 매트릭스를 판단하는 판별 로직을 완성하여 권한에 맞는 보안 통제를 달성한다.

## Tasks

<task id="19B-1">
<title>Repository 모델 개편 및 데이터베이스 마이그레이션</title>
<read_first>
- `Aristokeides.Api/Models/Repository.cs` — 기존 속성 확인
- `Aristokeides.Api/Data/AppDbContext.cs` — 기존 Repository 모델 맵 확인
</read_first>
<action>
1. `Repository.cs` 수정:
   - `OwnerId` 필드를 nullable로 변경: `public int? OwnerId { get; set; }`
   - `OrganizationId` 필드 추가: `public int? OrganizationId { get; set; }`
   - 네비게이션 프로퍼티 추가: `public Organization? Organization { get; set; }`
2. `AppDbContext.cs` 수정:
   - 기존의 `OwnerId` 기반 외래키 연동을 `IsRequired(false)` 형태로 명시.
   - `Organization` 과의 1:N 관계 매핑 및 외래키 `OrganizationId` 설정 추가 (삭제 행위 시 `DeleteBehavior.Cascade` 설정).
   - 유니크 인덱스 `(OwnerId, Name)`과 별개로 `(OrganizationId, Name)` 유니크 인덱스를 추가 정의.
3. EF Core 마이그레이션(Sqlite, Postgres, Mysql 각각)을 추가하고 적용한다:
   - `dotnet ef migrations add UpdateRepositoryOwnership --context SqliteAppDbContext -o Migrations/Sqlite`
   - `dotnet ef migrations add UpdateRepositoryOwnership --context PostgresAppDbContext -o Migrations/Postgres`
   - `dotnet ef migrations add UpdateRepositoryOwnership --context MysqlAppDbContext -o Migrations/Mysql`
</action>
<acceptance_criteria>
- Repository 모델 개편 및 마이그레이션 파일이 컴파일 에러 없이 작성된다.
- DB 마이그레이션이 로컬 SQLite 데이터베이스에 정상 적용된다.
</acceptance_criteria>
</task>

<task id="19B-2">
<title>저장소 생성 페이지(NewRepository.razor) 소유자 분기 추가</title>
<read_first>
- `Aristokeides.Api/Components/Pages/NewRepository.razor` — 현재 저장소 이름 및 중복 검증, 그리고 생성자 바인딩 확인
</read_first>
<action>
`NewRepository.razor` 파일을 수정하여 저장소 생성 시 소유자를 지정할 수 있도록 개선한다:

1. **소유 대상 로드 및 드롭다운:**
   - 사용자 초기화 시, 사용자가 소유자로 소속된 조직 목록(`Organizations` 중 사용자의 멤버 역할이 "Owner"인 목록)을 DB에서 가져온다.
   - UI 최상단에 "소유자(Owner)" 드롭다운 필드를 추가한다.
   - 옵션: `현재 사용자 (Me)`, `조직 목록...`
2. **이름 중복 검사 조건 분기:**
   - 개인 소유 저장소 선택 시: 기존 검증(`OwnerId == currentUserId && Name == name`) 유지.
   - 조직 소유 저장소 선택 시: `OrganizationId == selectedOrgId && Name == name` 인 리포지토리가 존재하는지 검사하여 충돌을 사전에 방지.
3. **저장소 생성 데이터 적재:**
   - 저장소 생성 DB 엔터티 작성 시:
     - 개인 선택: `OwnerId = currentUserId`, `OrganizationId = null`
     - 조직 선택: `OwnerId = null`, `OrganizationId = selectedOrgId`
4. **리다이렉션 경로 업데이트:**
   - 개인 소유: `/{username}/{reponame}`
   - 조직 소유: `/orgs/{orgname}/repos/{reponame}` 혹은 동일하게 뷰어 페이지로 올바르게 통합 리다이렉트.
</action>
<acceptance_criteria>
- 저장소 생성 화면에 소유자 선택 필드가 노출된다.
- 본인이 관리(Owner)하지 않는 다른 조직은 소유자 선택 목록에 나타나지 않아야 한다.
- 조직 소유 저장소 생성 시 DB 필드 `OrganizationId`가 올바르게 기록되고, 지정된 경로 상에 물리 베어 리포지토리가 생성된다.
</acceptance_criteria>
</task>

<task id="19B-3">
<title>물리 디렉토리 생성 워커 경로 수정</title>
<read_first>
- `Aristokeides.Api/Services/RepositoryCreationBackgroundWorker.cs` — 기존 물리 git repository 디렉토리 초기화 로직 확인
</read_first>
<action>
`RepositoryCreationBackgroundWorker.cs` 내의 리포지토리 생성 로직을 확인하여 소유자에 맞게 폴더가 생성되도록 수정한다:

- 기존에는 `GitRepos/{username}/{repoName}.git` 고정 경로를 사용함.
- 수정 상세:
  - 생성 대상 저장소를 로드할 때 `Owner` 및 `Organization` 정보를 함께 가져온다(`Include`).
  - 저장소의 소유자가 개인인 경우: `var ownerName = repo.Owner.Username;`
  - 저장소의 소유자가 조직인 경우: `var ownerName = repo.Organization.Name;`
  - 저장소 생성 물리 경로를 `Path.Combine(basePath, ownerName, repoName + ".git")`로 유연하게 결정하여 디렉토리 생성 및 `git init --bare` 명령이 수행되도록 처리한다.
</action>
<acceptance_criteria>
- 조직 소유 저장소 생성 시 물리적으로 `GitRepos/{조직명}/{저장소명}.git` 디렉토리가 성공적으로 구성된다.
</acceptance_criteria>
</task>

<task id="19B-4">
<title>Git Smart HTTP 미들웨어 권한 검증 개편</title>
<read_first>
- `Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs` — 기존 HTTP 권한 처리 검증부 참조 (L61-78)
</read_first>
<action>
`GitSmartHttpMiddleware.cs`를 수정하여 조직 저장소 및 다중 권한(RepositoryPermission) 구조에 맞게 접근 통제를 강화한다.

1. **리포지토리 조회 쿼리 수정:**
   - 요청 URL에서 추출한 `ownerOrOrgName`과 `repoName`으로 리포지토리를 조회한다.
   - 사용자가 개인인 경우 및 조직인 경우 모두 일치하도록 쿼리 작성:
     ```csharp
     var repo = await db.Repositories
         .Include(r => r.Owner)
         .Include(r => r.Organization)
         .FirstOrDefaultAsync(r => 
             (r.Owner != null && r.Owner.Username == ownerOrOrgName && r.Name == repoName) ||
             (r.Organization != null && r.Organization.Name == ownerOrOrgName && r.Name == repoName));
     ```
2. **동적 권한 레벨 판별 메서드 추가:**
   - 로그인된 사용자(`userId`)의 권한 등급을 식별한다.
   - **개인 소유:** `repo.OwnerId == userId` 인 경우 모든 권한(Admin)을 가진다.
   - **조직 소유:**
     - 사용자가 해당 조직의 소유자인지 확인: `db.OrganizationMembers.AnyAsync(om => om.OrganizationId == repo.OrganizationId && om.UserId == userId && om.Role == "Owner")` -> 참이면 Admin 등급 부여.
     - 그렇지 않은 경우, 사용자의 개별 권한 및 소속 팀 권한 조회:
       - 사용자가 소속된 조직 내 팀 ID 목록 조회: `db.TeamMembers.Where(tm => tm.UserId == userId && tm.Team.OrganizationId == repo.OrganizationId).Select(tm => tm.TeamId).ToListAsync()`
       - `RepositoryPermission` 테이블에서 `RepositoryId == repo.Id` 이고, (`UserId == userId` 이거나 `TeamId`가 사용자의 팀 목록에 포함된) 모든 권한 중 최상위 등급을 선택한다 ("Admin" > "Write" > "Read").
   - 권한 등급 판정:
     - 비공개 저장소 읽기 시도 시: 최소 `Read` 권한 이상 필요. (권한이 없는 사용자는 403 Forbidden 반환)
     - 쓰기(Push) 시도 시: 최소 `Write` 권한 이상 필요. (권한이 없는 사용자는 403 Forbidden 반환)
</action>
<acceptance_criteria>
- 조직 소유 비공개 저장소에 권한이 없는 사용자가 HTTP clone/pull을 시도하면 403 Forbidden 또는 404가 발생해야 한다.
- 읽기 전용(`Read`) 권한을 가진 사용자/팀원이 push를 시도하면 403 Forbidden으로 안전하게 차단되어야 한다.
- `Write` 권한 이상의 사용자/팀원은 push 및 pull을 모두 정상 수행할 수 있어야 한다.
</acceptance_criteria>
</task>

<task id="19B-5">
<title>SSH 인증 서비스(SshServerBackgroundService) 권한 검증 개편</title>
<read_first>
- `Aristokeides.Api/Services/Ssh/SshServerBackgroundService.cs` — 기존 SSH Command 인증 및 권한 확인 로직 참조 (L250-275)
</read_first>
<action>
`SshServerBackgroundService.cs` 내의 리포지토리 권한 확인 로직을 Git HTTP와 동일한 권한 매트릭스로 검증하도록 수정한다:

1. **리포지토리 식별 및 조회 개편:**
   - 추출한 `ownerName`(개인 또는 조직)과 `repoName`으로 리포지토리를 조회한다.
     ```csharp
     var repository = await dbContext.Repositories
         .Include(r => r.Owner)
         .Include(r => r.Organization)
         .FirstOrDefaultAsync(r => 
             (r.Owner != null && r.Owner.Username == ownerName && r.Name == repoName) ||
             (r.Organization != null && r.Organization.Name == ownerName && r.Name == repoName));
     ```
2. **권한 확인 로직 개편 (HTTP와 동일한 로직):**
   - 사용자가 저장소 소유자인지, 또는 조직의 소유자인지 확인하여 참인 경우 패스.
   - 그렇지 않은 경우, `RepositoryPermission`을 조회하여 사용자의 팀 권한 및 개별 사용자 권한 중 최상위 권한 등급("Admin", "Write", "Read")을 계산한다.
   - 명령어 종류 분석:
     - `git-upload-pack`(Pull/Fetch): 비공개 저장소의 경우 최소 `Read` 권한 이상 필요. 없을 시 `Permission denied` 반환 및 중단.
     - `git-receive-pack`(Push): 최소 `Write` 권한 이상 필요. 없을 시 `Permission denied` 반환 및 중단.
</action>
<acceptance_criteria>
- SSH를 통해 조직 비공개 저장소 클론 및 푸시 시도 시, 권한 등급에 따라 정상적으로 성공하거나 거부(`Permission denied`)된다.
</acceptance_criteria>
</task>

## must_haves

- `Repository` 소유권 개편에 따라 기존의 개인 저장소 정보 및 물리 디렉토리 경로에 부작용(Side Effect)이 발생하지 않아야 한다.
- Git HTTP/SSH 권한 검사 시 조직 소유자의 Admin 마스터 권한이 우선적으로 유효해야 한다.
- 비공개 조직 저장소에 대한 접근 권한 검사 시 개별 권한과 소속 팀 권한의 합집합 중 최상위 권한이 적용되어야 한다.

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Models/Repository.cs` | 수정 | 조직 소유권 대응 외래키 관계 추가 |
| `Aristokeides.Api/Data/AppDbContext.cs` | 수정 | Repository 관계 설정 및 유니크 조건 변경 |
| `Aristokeides.Api/Components/Pages/NewRepository.razor` | 수정 | 소유자 지정 및 조직별 저장소명 고유 검증 추가 |
| `Aristokeides.Api/Services/RepositoryCreationBackgroundWorker.cs` | 수정 | 조직 폴더 및 저장소 베어 폴더 초기화 물리 경로 변경 |
| `Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs` | 수정 | HTTP push/pull 권한 분석 모듈 통합 개편 |
| `Aristokeides.Api/Services/Ssh/SshServerBackgroundService.cs` | 수정 | SSH push/pull 명령 권한 검증 모듈 개편 |
