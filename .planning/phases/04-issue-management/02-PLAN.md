---
wave: 2
depends_on: [01-PLAN.md]
files_modified:
  - Aristokeides.Api/Services/IssueService.cs
  - Aristokeides.Api/Program.cs
  - Aristokeides.Api/Components/Pages/RepoIssues.razor
  - Aristokeides.Api/Components/Pages/RepoIssueDetail.razor
  - Aristokeides.Api/Components/Pages/RepoIssueForm.razor
autonomous: true
---

# Phase 4 Plan - Wave 2: Issues UI & Interaction

<threat_model>
- **Authorization Bypass**: Ensure only users with appropriate access to a repository can view/create issues.
- **IDOR (Insecure Direct Object Reference)**: When updating an issue, verify the issue belongs to the repository the user is viewing.
- **XSS**: Issue titles and descriptions must be safely rendered by Blazor.
</threat_model>

<tasks>

  <task id="issue-service" status="todo">
    <description>Create IssueService for logic and DB access</description>
    <requirements>ISSU-01, ISSU-02</requirements>
    <read_first>
      - Aristokeides.Api/Models/Issue.cs
      - Aristokeides.Api/Models/BoardColumn.cs
    </read_first>
    <action>
      1. Create `Aristokeides.Api/Services/IssueService.cs`.
      2. Implement methods: `GetIssuesAsync(Guid repositoryId)`, `GetBoardColumnsAsync(Guid repositoryId)`, `GetIssueAsync(Guid repositoryId, int localId)`.
      3. Implement `CreateIssueAsync`: Fetch max `LocalId` for the `repositoryId` with `MAX()` lock/query, assign `LocalId = max + 1`, and default `ColumnId` to the "To Do" column.
      4. Implement `UpdateIssueStatusAsync(Guid issueId, Guid newColumnId)` to support drag-and-drop.
      5. Implement `UpdateIssueDetailsAsync(Guid issueId, string title, string description, Guid? assigneeId)` and `CloseIssueAsync(Guid issueId)` methods to support issue editing and closing.
      6. Register `IssueService` as Scoped in `Program.cs`.
    </action>
    <acceptance_criteria>
      - `IssueService` exposes required CRUD operations.
      - `LocalId` increments per repository starting from 1.
      - `Program.cs` registers the service.
    </acceptance_criteria>
  </task>

  <task id="kanban-ui" status="todo">
    <description>Implement Kanban Board View</description>
    <requirements>ISSU-02</requirements>
    <read_first>
      - .planning/phases/04-issue-management/04-UI-SPEC.md
    </read_first>
    <action>
      1. Create `RepoIssues.razor` with `@page "/{username}/{repoName}/issues"`.
      2. Add `@rendermode InteractiveServer`.
      3. Fetch `BoardColumns` and `Issues` using `IssueService`.
      4. Implement Kanban UI: Flex container with fixed-width columns. Render issue cards inside columns.
      5. Implement HTML5 Drag and Drop events (`@ondragstart`, `@ondrop`, `@ondragover:preventDefault`) on cards and columns.
      6. Update DB on drop via `IssueService.UpdateIssueStatusAsync`.
    </action>
    <acceptance_criteria>
      - Kanban board renders columns and correctly distributes issues.
      - Drag and drop successfully updates the issue state in DB and UI immediately reflects it.
    </acceptance_criteria>
  </task>

  <task id="issue-forms" status="todo">
    <description>Implement Issue Creation and Detail Views</description>
    <requirements>ISSU-01</requirements>
    <read_first>
      - Aristokeides.Api/Services/IssueService.cs
    </read_first>
    <action>
      1. Create `RepoIssueForm.razor` (`@page "/{username}/{repoName}/issues/new"`): Form with Title, Description, and Create button. Redirect to the issue list upon success.
      2. Create `RepoIssueDetail.razor` (`@page "/{username}/{repoName}/issues/{localId:int}"`): Show issue details, allow editing Title/Description/Assignee, and provide a "Close Issue" button.
      3. Integrate with `IssueService`.
    </action>
    <acceptance_criteria>
      - A new issue can be created and it appears on the Kanban board.
      - Clicking an issue on the Kanban board navigates to its detail view.
      - Issue detail view allows editing and state change (e.g., closing).
    </acceptance_criteria>
  </task>

</tasks>

<must_haves>
  - InteractiveServer mode successfully enables drag-and-drop.
  - Kanban columns reflect the ones seeded in DB.
  - ISSU-01 and ISSU-02 requirements are fully verified.
</must_haves>
