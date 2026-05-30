---
phase: 05
plan: 01
objective: PR Data Model, Service & Creation UI
wave: 1
depends_on: []
requirements: [CODE-01]
files_modified:
  - Aristokeides.Api/Models/PullRequest.cs
  - Aristokeides.Api/Models/Issue.cs
  - Aristokeides.Api/Models/IssueComment.cs
  - Aristokeides.Api/Data/AppDbContext.cs
  - Aristokeides.Api/Services/PullRequestService.cs
  - Aristokeides.Api/Components/Pages/RepoPullRequests.razor
  - Aristokeides.Api/Components/Pages/RepoPullRequestForm.razor
autonomous: true
---

# Phase 05 Plan 01: PR Data Model, Service & Creation UI

## 1. Description
이 플랜은 Phase 05의 첫 번째 부분으로, Issue와 PR을 통합하는 데이터 모델(`PullRequest`, `IssueComment`)을 설계하고 Entity Framework Core 컨텍스트에 반영합니다. 또한 `PullRequestService`를 통해 새 PR을 생성하고, Blazor UI를 통해 저장소 내의 PR 목록을 조회하며 새 PR을 작성(Source/Target 브랜치 선택)하는 폼을 구현합니다.

## 2. Tasks

```xml
<task id="1" type="execute">
  <action>Create Models and Update AppDbContext</action>
  <read_first>
    <file>Aristokeides.Api/Models/Issue.cs</file>
    <file>Aristokeides.Api/Data/AppDbContext.cs</file>
    <file>.planning/phases/05-prs-code-review/05-RESEARCH.md</file>
  </read_first>
  <description>
    1. Create `PullRequest.cs` mapped 1:1 to `Issue` (using `IssueId` as Key and FK). Add `SourceBranch`, `TargetBranch`, `IsMerged`, and `MergeCommitSha`.
    2. Create `IssueComment.cs` mapping to `IssueId` and `AuthorId`, adding `Content` and `CreatedAt`.
    3. Update `Issue.cs` with `public PullRequest? PullRequest { get; set; }` and `public ICollection&lt;IssueComment&gt; Comments { get; set; } = new List&lt;IssueComment&gt;();`.
    4. Update `AppDbContext.cs` adding `DbSet&lt;PullRequest&gt;` and `DbSet&lt;IssueComment&gt;`.
    5. Configure `AppDbContext.OnModelCreating` to map the 1:1 relationship between Issue and PullRequest, and 1:N for Issue-IssueComments. Set DeleteBehavior properly (e.g. Cascade for PullRequest, Restrict for IssueComment's Author).
  </description>
  <acceptance_criteria>
    - `PullRequest.cs` and `IssueComment.cs` exist with correct properties.
    - `AppDbContext.cs` contains `DbSets` for the new models.
    - `dotnet build` succeeds without compilation errors.
  </acceptance_criteria>
</task>

<task id="2" type="execute">
  <action>[BLOCKING] Apply EF Core Migrations</action>
  <read_first>
    <file>Aristokeides.Api/Data/AppDbContext.cs</file>
  </read_first>
  <description>
    This phase modifies schema-relevant files (EF Core models). Create and apply the database migration.
    1. Run `dotnet ef migrations add Phase5_PullRequests --project Aristokeides.Api`
    2. Run `dotnet ef database update --project Aristokeides.Api`
  </description>
  <acceptance_criteria>
    - `dotnet ef database update` completes successfully.
    - New migration file is created in `Migrations` folder.
  </acceptance_criteria>
</task>

<task id="3" type="execute">
  <action>Implement PullRequestService (Creation and Validation)</action>
  <read_first>
    <file>Aristokeides.Api/Services/IssueService.cs</file>
    <file>Aristokeides.Api/Services/PullRequestService.cs</file>
  </read_first>
  <description>
    Create `PullRequestService` to handle PR operations.
    1. Inject `AppDbContext`, `IssueService`, and `GitRepositoryService` (or equivalent LibGit2Sharp wrapper).
    2. Create `CreatePullRequestAsync` method that validates if Source/Target branches exist.
    3. It should first call `IssueService.CreateIssueAsync` (to get the `LocalId` and `IssueId`), and then insert a `PullRequest` record mapped to that `IssueId`.
    4. Provide `GetPullRequestsAsync(repositoryId)` to list only issues that have an associated `PullRequest`.
  </description>
  <acceptance_criteria>
    - `PullRequestService.cs` is created and registered in `Program.cs`.
    - `CreatePullRequestAsync` orchestrates `IssueService` to create the parent `Issue` and persists a `PullRequest`.
  </acceptance_criteria>
</task>

<task id="4" type="execute">
  <action>Implement Pull Request List UI</action>
  <read_first>
    <file>Aristokeides.Api/Components/Pages/RepoPullRequests.razor</file>
    <file>.planning/phases/05-prs-code-review/05-UI-SPEC.md</file>
  </read_first>
  <description>
    Create `RepoPullRequests.razor` (mapped to `/{username}/{repoName}/pulls`).
    1. Display a list of PRs using `PullRequestService.GetPullRequestsAsync`.
    2. Show PR title, `#LocalId`, Source branch -> Target branch, and Status (Open, Merged).
    3. Include a "Create Pull Request" button pointing to `/{username}/{repoName}/pulls/new`.
  </description>
  <acceptance_criteria>
    - Navigation to `/{username}/{repoName}/pulls` loads without error.
    - PRs are displayed if they exist, or empty state text matches `05-UI-SPEC.md`.
  </acceptance_criteria>
</task>

<task id="5" type="execute">
  <action>Implement PR Creation Form UI</action>
  <read_first>
    <file>Aristokeides.Api/Components/Pages/RepoPullRequestForm.razor</file>
    <file>.planning/phases/05-prs-code-review/05-UI-SPEC.md</file>
  </read_first>
  <description>
    Create `RepoPullRequestForm.razor` (mapped to `/{username}/{repoName}/pulls/new`).
    1. Fetch available branches from the Git repository.
    2. Form inputs: Title, Description, Source Branch Dropdown, Target Branch Dropdown.
    3. On submit, call `PullRequestService.CreatePullRequestAsync`.
    4. Upon success, redirect to `/{username}/{repoName}/pulls/{localId}`.
  </description>
  <acceptance_criteria>
    - User can navigate to `/{username}/{repoName}/pulls/new`.
    - Submitting the form persists a new PR/Issue in the database and redirects properly.
  </acceptance_criteria>
</task>
```

## 3. Verification Criteria
- `dotnet ef migrations list` shows the new PullRequest migration.
- `dotnet run` works and the application starts.
- User can navigate to `/{username}/{repoName}/pulls` and click "Create Pull Request".
- Creating a PR successfully saves a new issue with an associated `PullRequest` object in the DB.

## 4. Must Haves
- **Truth 1**: PR LocalId matches Issue LocalId (shared sequence).
- **Truth 2**: EF Core database migration must be executed after adding `PullRequest` and `IssueComment` entities.
