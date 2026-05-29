---
phase: 2
slug: core-git-operations
mode: mvp
requirements: REPO-01, REPO-02
---

# Phase 2: Core Git Operations - Execution Plan

## Verification Criteria
- [ ] Tests stubbed in Wave 0 pass with `dotnet test`.
- [ ] The database has a `Repository` table and `User` table has a `Username` column.
- [ ] LibGit2Sharp is configured and successfully initializes a bare git repository on disk upon API request.
- [ ] Git Smart HTTP endpoints correctly invoke `git-http-backend` and parse CGI output stream.
- [ ] Git authentication (Basic Auth) accepts the user's email and password.

## Must Haves
- The database schema push task MUST be executed using `dotnet ef database update`.
- `[BLOCKING]` task for schema push is present and correctly mapped.
- All actions have explicit executable details (concrete values).

## Tasks

```xml
<task id="02-00-01" wave="0" depends_on="" files_modified="Aristokeides.Tests/RepositoriesControllerTests.cs, Aristokeides.Tests/GitSmartHttpTests.cs" autonomous="true">
  <action>Create stub test files `RepositoriesControllerTests.cs` and `GitSmartHttpTests.cs` inside the `Aristokeides.Tests` project to fulfill Wave 0 validation requirements.</action>
  <read_first>E:/Workspace/VisualC#/Aristokeides/.planning/phases/02-core-git-operations/02-VALIDATION.md</read_first>
  <acceptance_criteria>The files `RepositoriesControllerTests.cs` and `GitSmartHttpTests.cs` exist with basic dummy tests (e.g., Assert.True(true)), and `dotnet test` passes.</acceptance_criteria>
</task>

<task id="02-01-01" wave="1" depends_on="02-00-01" files_modified="Aristokeides.Api/Models/User.cs, Aristokeides.Api/Models/Repository.cs" autonomous="true">
  <action>Add a `public string Username { get; set; }` property to the existing `User` model. Create a new `Repository` model with properties `Guid Id`, `string Name`, `string Description`, `Guid OwnerId`, `string Status` (enum or string with Creating, Ready, Error), and `DateTime CreatedAt`. Add `User Owner` and `ICollection<Repository> Repositories` navigation properties.</action>
  <read_first>Aristokeides.Api/Models/User.cs, E:/Workspace/VisualC#/Aristokeides/.planning/phases/02-core-git-operations/02-RESEARCH.md</read_first>
  <acceptance_criteria>`User.cs` has a `Username` property. `Repository.cs` is created with the required properties. Models compile without errors.</acceptance_criteria>
</task>

<task id="02-01-02" wave="1" depends_on="02-01-01" files_modified="Aristokeides.Api/Data/AppDbContext.cs" autonomous="true">
  <action>Add `public DbSet<Repository> Repositories { get; set; }` to `AppDbContext.cs`. Configure the one-to-many relationship between `User` and `Repository` in `OnModelCreating`, and add a unique constraint on `Username` for `User`.</action>
  <read_first>Aristokeides.Api/Data/AppDbContext.cs</read_first>
  <acceptance_criteria>`AppDbContext` contains the `Repositories` DbSet and correctly maps the entities with EF Core fluent API.</acceptance_criteria>
</task>

<task id="02-01-03" wave="1" depends_on="02-01-02" files_modified="" autonomous="true">
  <action>Run command `dotnet ef migrations add Phase2CoreGitOps --project Aristokeides.Api` to create the EF Core migration for the newly added `Repository` model and `Username` field.</action>
  <read_first>Aristokeides.Api/Data/AppDbContext.cs</read_first>
  <acceptance_criteria>A new migration file is generated inside the `Migrations` folder without compilation errors.</acceptance_criteria>
</task>

<task id="02-01-04" wave="1" depends_on="02-01-03" files_modified="" autonomous="false">
  <action>[BLOCKING] Run `dotnet ef database update --project Aristokeides.Api` to push the schema modifications to the database.</action>
  <read_first>E:/Workspace/VisualC#/Aristokeides/.planning/phases/02-core-git-operations/02-RESEARCH.md</read_first>
  <acceptance_criteria>The database schema is updated successfully. The `Repository` table is present in the local database and the `User` table contains the `Username` column.</acceptance_criteria>
</task>

<task id="02-02-01" wave="2" depends_on="02-01-04" files_modified="Aristokeides.Api/Aristokeides.Api.csproj" autonomous="true">
  <action>Run `dotnet add Aristokeides.Api package LibGit2Sharp` to add the LibGit2Sharp dependency to the project.</action>
  <read_first>Aristokeides.Api/Aristokeides.Api.csproj</read_first>
  <acceptance_criteria>The `Aristokeides.Api.csproj` file contains a PackageReference for `LibGit2Sharp`.</acceptance_criteria>
</task>

<task id="02-02-02" wave="2" depends_on="02-02-01" files_modified="Aristokeides.Api/Services/RepositoryCreationChannel.cs, Aristokeides.Api/Services/RepositoryCreationBackgroundWorker.cs, Aristokeides.Api/Program.cs" autonomous="true">
  <action>Implement a Channel-based task queue `RepositoryCreationChannel` using `Channel.CreateUnbounded<Guid>()`. Implement `RepositoryCreationBackgroundWorker` as a `BackgroundService`. The worker should read IDs from the channel, query the `Repository` and its `User.Username` from DB using a scoped service, and call `LibGit2Sharp.Repository.Init($"C:/GitRepos/{username}/{repoName}.git", isBare: true)`. Finally, update the repository status to 'Ready'. Register the Channel as Singleton and the Worker as a HostedService in `Program.cs`.</action>
  <read_first>E:/Workspace/VisualC#/Aristokeides/.planning/phases/02-core-git-operations/02-RESEARCH.md</read_first>
  <acceptance_criteria>The background service continuously listens on the channel, successfully initializes a bare git repo via LibGit2Sharp when enqueued, and updates the entity state.</acceptance_criteria>
</task>

<task id="02-03-01" wave="3" depends_on="02-02-02" files_modified="Aristokeides.Api/Auth/BasicAuthenticationHandler.cs, Aristokeides.Api/Program.cs" autonomous="true">
  <action>Create `BasicAuthenticationHandler.cs` that extends `AuthenticationHandler<AuthenticationSchemeOptions>`. Override `HandleAuthenticateAsync` to parse the `Authorization: Basic` header, extract email and password, query the DB for the User, and verify the password using `BCrypt.Net.BCrypt.Verify`. If valid, generate a `ClaimsPrincipal`. Register it as the "Basic" scheme in `Program.cs`.</action>
  <read_first>E:/Workspace/VisualC#/Aristokeides/.planning/phases/02-core-git-operations/02-RESEARCH.md</read_first>
  <acceptance_criteria>`BasicAuthenticationHandler` properly authenticates requests bearing valid base64 email:password pairs and rejects invalid ones.</acceptance_criteria>
</task>

<task id="02-03-02" wave="3" depends_on="02-03-01" files_modified="Aristokeides.Api/Controllers/RepositoriesController.cs" autonomous="true">
  <action>Create `RepositoriesController.cs` under the `api/repositories` route. Add a `[HttpPost]` endpoint that expects a JSON body with `name` and `description`. Validate the input. Extract the user ID from claims, insert a new `Repository` entity with status `Creating`, enqueue the new ID into the `RepositoryCreationChannel`, and return `202 Accepted`.</action>
  <read_first>E:/Workspace/VisualC#/Aristokeides/.planning/phases/02-core-git-operations/02-RESEARCH.md</read_first>
  <acceptance_criteria>A POST request creates a database entry in `Creating` state, sends the ID to the background worker, and responds with a 202 status code.</acceptance_criteria>
</task>

<task id="02-04-01" wave="4" depends_on="02-03-02" files_modified="Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs, Aristokeides.Api/Program.cs" autonomous="true">
  <action>Create `GitSmartHttpMiddleware.cs` for proxying git traffic to `git.exe http-backend`. Match paths like `/{username}/{repo.name}.git/{*path}`. Ensure the user is authenticated via Basic Auth. Launch a `Process` for `git.exe http-backend`, setting env vars `GIT_PROJECT_ROOT`, `PATH_INFO`, and `GIT_HTTP_EXPORT_ALL=1`. Stream `context.Request.Body` into the process's standard input. Read from the standard output: parse HTTP headers until an empty line, write them to `context.Response.Headers`, and stream the remaining binary payload to `context.Response.Body`. Map the middleware in `Program.cs`.</action>
  <read_first>E:/Workspace/VisualC#/Aristokeides/.planning/phases/02-core-git-operations/02-RESEARCH.md</read_first>
  <acceptance_criteria>Git clients can successfully perform `git clone`, `git push`, and `git pull` through HTTP using standard git client commands.</acceptance_criteria>
</task>
```
