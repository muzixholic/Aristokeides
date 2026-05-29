---
wave: 1
depends_on: []
files_modified:
  - Aristokeides.Api/Models/BoardColumn.cs
  - Aristokeides.Api/Models/Issue.cs
  - Aristokeides.Api/Models/User.cs
  - Aristokeides.Api/Models/Repository.cs
  - Aristokeides.Api/Data/AppDbContext.cs
  - Aristokeides.Api/Controllers/RepositoriesController.cs
  - Aristokeides.Api/Program.cs
  - Aristokeides.Api/Components/App.razor
autonomous: true
---

# Phase 4 Plan - Wave 1: Schema & Backend Infrastructure

<threat_model>
- **Authorization Bypass**: Ensure only users with appropriate access to a repository can view/create issues.
- **IDOR (Insecure Direct Object Reference)**: When updating an issue, verify the issue belongs to the repository the user is viewing.
- **SQL Injection**: Prevented by using EF Core LINQ methods safely.
- **XSS**: Issue titles and descriptions must be properly sanitized or safely rendered by Blazor.
</threat_model>

<tasks>

  <task id="model-and-db" status="todo">
    <description>Create BoardColumn and Issue models and configure AppDbContext</description>
    <requirements>ISSU-01, ISSU-02</requirements>
    <read_first>
      - Aristokeides.Api/Data/AppDbContext.cs
      - Aristokeides.Api/Models/Repository.cs
      - Aristokeides.Api/Models/User.cs
    </read_first>
    <action>
      1. Create `Aristokeides.Api/Models/BoardColumn.cs` with Id (Guid), RepositoryId (Guid), Name (string), Order (int). Add navigation property to Repository.
      2. Create `Aristokeides.Api/Models/Issue.cs` with Id (Guid), LocalId (int), RepositoryId (Guid), Title (string), Description (string), CreatorId (int), AssigneeId (int?), ColumnId (Guid), CreatedAt, UpdatedAt. Add navigation properties.
      3. Update `User.cs` and `Repository.cs` to add necessary ICollection navigation properties (like `Issues`, `BoardColumns` for Repo).
      4. Add `DbSet<BoardColumn>` and `DbSet<Issue>` to `AppDbContext.cs`.
      5. Configure EF Core relationships and constraints in `OnModelCreating` (e.g., DeleteBehavior.Cascade for repository deletion).
      6. Run EF Core CLI to add migration `Phase4IssueManagement`.
    </action>
    <acceptance_criteria>
      - `BoardColumn` and `Issue` classes exist with proper properties.
      - `AppDbContext` has DbSets and configurations for the new entities.
      - A new migration file is generated successfully in the `Migrations` folder.
    </acceptance_criteria>
  </task>

  <task id="update-program-and-seeding" status="todo">
    <description>Enable Blazor InteractiveServer and seed BoardColumns on repo creation</description>
    <requirements>ISSU-01, ISSU-02</requirements>
    <read_first>
      - Aristokeides.Api/Program.cs
      - Aristokeides.Api/Components/App.razor
      - Aristokeides.Api/Controllers/RepositoriesController.cs
    </read_first>
    <action>
      1. In `Program.cs`, append `.AddInteractiveServerComponents()` to `AddRazorComponents()`.
      2. In `Program.cs`, append `.AddInteractiveServerRenderMode()` to `MapRazorComponents<App>()`.
      3. In `App.razor`, ensure `<Routes />` can support InteractiveServer if needed or prepare `<HeadOutlet />` (already standard in .NET 8/10, just verify if needed). *Wait, usually InteractiveServer is enabled via attribute in specific pages.*
      4. In `RepositoriesController.Create`, before `_db.SaveChangesAsync()`, create 3 `BoardColumn` instances for the new repo: "To Do" (Order 1), "In Progress" (Order 2), "Done" (Order 3). Add them to `_db.BoardColumns`.
    </action>
    <acceptance_criteria>
      - `Program.cs` properly configures InteractiveServer.
      - `RepositoriesController.cs` seeds 3 default BoardColumns on repository creation.
    </acceptance_criteria>
  </task>

</tasks>

<must_haves>
  - Database migration matches schema intent.
  - New Repositories automatically get 3 default columns.
</must_haves>
