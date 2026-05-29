---
wave: 1
depends_on: []
files_modified: []
autonomous: true
---

# Phase 3 Plan: Repository Browsing

## Overview
This phase introduces the web interface for viewing repositories, branches, files, and commit history. It integrates Blazor Server (SSR mode) into the existing API project and configures Cookie authentication to coexist with the current JWT setup.

## Must Haves (Verification)
- [ ] Navigating to `/login` allows the user to log in via a web form and sets a session cookie.
- [ ] Authenticated users can navigate to `/{username}/{repoName}` to view the repository root directory.
- [ ] Users can navigate into subdirectories and click files to view their source code.
- [ ] Source code view has basic syntax highlighting via highlight.js.
- [ ] Users can view the commit history for a branch at `/{username}/{repoName}/commits/{branch}` with pagination (Previous/Next).
- [ ] Unauthorized users or users without access to a repository are correctly blocked.

## Requirements Covered
- REPO-03

## Execution Waves

### Wave 1: UI Foundation and Auth Integration

```xml
<task>
  <id>w1-t1</id>
  <description>Setup Static Files and Blazor Server (SSR) Foundation</description>
  <action>
    - Edit `Aristokeides.Api/Program.cs` to add `builder.Services.AddRazorComponents()` and `builder.Services.AddAntiforgery()`. Add `app.UseStaticFiles()` and `app.UseAntiforgery()`. Map components with `app.MapRazorComponents<App>()`.
    - Create directory `Aristokeides.Api/wwwroot/css` and add `app.css`. Define CSS variables from the UI-SPEC (e.g., `--accent: #2563EB`, `--destructive: #EF4444`, basic layout styles).
    - Create `Aristokeides.Api/Components/App.razor` with standard HTML structure, injecting `app.css` and `highlight.js` CDN scripts.
    - Create `Aristokeides.Api/Components/Routes.razor`, `Aristokeides.Api/Components/MainLayout.razor` (with a basic navbar linking to home/logout), and `Aristokeides.Api/Components/_Imports.razor`.
  </action>
  <read_first>
    - E:/Workspace/VisualC#/Aristokeides/.planning/phases/03-repository-browsing/03-UI-SPEC.md
    - E:/Workspace/VisualC#/Aristokeides/Aristokeides.Api/Program.cs
  </read_first>
  <acceptance_criteria>
    - The API project successfully compiles.
    - Accessing `wwwroot/css/app.css` returns the stylesheet.
    - Navigating to the root renders the Blazor layout.
  </acceptance_criteria>
</task>

<task>
  <id>w1-t2</id>
  <description>Configure Cookie Authentication and Login Page</description>
  <action>
    - Edit `Aristokeides.Api/Program.cs`: Add `.AddCookie("Cookies", opts => opts.LoginPath = "/login")`. Add `.AddPolicyScheme("JWT_OR_COOKIE", "JWT_OR_COOKIE", opts => ...)` to dynamically select JWT (if `Authorization: Bearer` is present) or Cookies. Set the default schemes to `JWT_OR_COOKIE`. Add `builder.Services.AddCascadingAuthenticationState()`.
    - Edit `Aristokeides.Api/Controllers/AuthController.cs`: Add `[HttpPost("cookie-login")]` that takes `[FromForm] string email, [FromForm] string password`. Validate credentials via DB, call `HttpContext.SignInAsync("Cookies", claimsPrincipal)`, and redirect to `/`.
    - Create `Aristokeides.Api/Components/Pages/Login.razor` mapped to `@page "/login"`. Implement a standard HTML `<form method="post" action="/api/auth/cookie-login">` containing `email` and `password` inputs and an Antiforgery token `<AntiforgeryToken />`.
  </action>
  <read_first>
    - E:/Workspace/VisualC#/Aristokeides/Aristokeides.Api/Controllers/AuthController.cs
    - E:/Workspace/VisualC#/Aristokeides/Aristokeides.Api/Program.cs
  </read_first>
  <acceptance_criteria>
    - Submitting valid credentials on `/login` sets a cookie and redirects.
    - Accessing an `[Authorize]` route with the cookie succeeds.
    - API endpoints continue to accept JWT.
  </acceptance_criteria>
</task>
```

### Wave 2: Git Service for UI

```xml
<task>
  <id>w2-t1</id>
  <description>Implement GitBrowserService</description>
  <action>
    - Create `Aristokeides.Api/Services/GitBrowserService.cs` and register it as Scoped in `Program.cs`.
    - Implement `ValidateAccessAsync(int userId, string username, string repoName)` which queries `AppDbContext` to ensure the repo exists and belongs to the user. Returns the local repository path.
    - Implement `GetBranches(string repoPath)`: Returns a list of all branch names.
    - Implement `GetDefaultBranch(string repoPath)`: Returns the default branch name (e.g., `repo.Head.FriendlyName`).
    - Implement `GetTreeEntries(string repoPath, string branch, string path)`: Returns a list of tree entries (name, isFolder). Use `LibGit2Sharp.Repository`.
    - Implement `GetBlobContent(string repoPath, string branch, string path)`: Returns the string content of a file.
    - Implement `GetCommits(string repoPath, string branch, int page, int pageSize)`: Returns paginated commit info (hash, message, author, date).
  </action>
  <read_first>
    - E:/Workspace/VisualC#/Aristokeides/.planning/phases/03-repository-browsing/03-RESEARCH.md
  </read_first>
  <acceptance_criteria>
    - `ValidateAccessAsync` correctly blocks unauthorized access.
    - Git operations successfully read from the `C:/GitRepos/{username}/{repoName}.git` path.
    - Can retrieve the list of branches and the default branch.
  </acceptance_criteria>
</task>
```

### Wave 3: Repository Browsing Component

```xml
<task>
  <id>w3-t1</id>
  <description>Implement Tree View Component</description>
  <action>
    - Create `Aristokeides.Api/Components/Pages/RepoBrowser.razor`.
    - Route `@page "/{username}/{repoName}"`, `@page "/{username}/{repoName}/tree/{branch}"`, `@page "/{username}/{repoName}/tree/{branch}/{*path}"`.
    - Add `@attribute [Authorize]`. Inject `AuthenticationStateProvider` and `GitBrowserService`.
    - On init, validate access. If `branch` parameter is null, resolve it using `GitBrowserService.GetDefaultBranch()` and proceed. Fetch tree entries using `GitBrowserService.GetTreeEntries()`.
    - Render a branch selector dropdown using `GitBrowserService.GetBranches()`. When changed, it should navigate to `/tree/{selectedBranch}`.
    - Render a UI navigation link/tab to the Commits page: `<a href="/{username}/{repoName}/commits/{branch}">Commits</a>`.
    - Render breadcrumbs for the current path.
    - Render a table/list of items. Folders link to `/tree/{branch}/{newPath}`, files link to `/blob/{branch}/{newPath}`.
    - If the repository is empty (no commits), show "빈 저장소입니다" and instructions.
  </action>
  <read_first>
    - E:/Workspace/VisualC#/Aristokeides/Aristokeides.Api/Services/GitBrowserService.cs
  </read_first>
  <acceptance_criteria>
    - Accessing the root URL of a repository automatically resolves the default branch.
    - Users can view the branch list and switch between branches.
    - The commits page link is visible and navigates to the correct branch's commit history.
    - Clicking a folder navigates correctly to its contents.
    - Handles empty repositories gracefully.
  </acceptance_criteria>
</task>
```

### Wave 4: File Content & Commits Components

```xml
<task>
  <id>w4-t1</id>
  <description>Implement File Viewer (Blob) Component</description>
  <action>
    - Create `Aristokeides.Api/Components/Pages/RepoBlob.razor`.
    - Route `@page "/{username}/{repoName}/blob/{branch}/{*path}"`.
    - Add `@attribute [Authorize]`. Validate access and fetch file content using `GitBrowserService`.
    - Render content inside `<pre><code class="language-{ext}">@content</code></pre>`.
    - Since this is SSR, include an inline `<script>` below the pre block that calls `hljs.highlightElement(document.querySelector('pre code'))` to trigger syntax highlighting on page load.
  </action>
  <read_first>
    - E:/Workspace/VisualC#/Aristokeides/.planning/phases/03-repository-browsing/03-UI-SPEC.md
  </read_first>
  <acceptance_criteria>
    - File contents are correctly displayed.
    - Syntax highlighting is applied to the code block via highlight.js.
  </acceptance_criteria>
</task>

<task>
  <id>w4-t2</id>
  <description>Implement Commit History Component</description>
  <action>
    - Create `Aristokeides.Api/Components/Pages/RepoCommits.razor`.
    - Route `@page "/{username}/{repoName}/commits/{branch}"`.
    - Support pagination via query string `?page={pageNumber}`.
    - Add `@attribute [Authorize]`. Fetch commits using `GitBrowserService`.
    - Render a list/table of commits showing hash, message, author, and date.
    - Add "Previous" and "Next" link buttons depending on page number and availability of more commits.
  </action>
  <read_first>
    - E:/Workspace/VisualC#/Aristokeides/.planning/phases/03-repository-browsing/03-RESEARCH.md
  </read_first>
  <acceptance_criteria>
    - Displays correct commit history for the specified branch.
    - Pagination links work correctly to navigate older/newer commits.
  </acceptance_criteria>
</task>
```
