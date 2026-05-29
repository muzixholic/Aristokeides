# Phase 3: Repository Browsing - Research

## Overview
This phase introduces the first Web UI components to the project. The goal is to allow users to browse repository files, view commit histories, and see file contents with syntax highlighting using Blazor.

## 1. Blazor Web App Foundation
- **Current State**: `Aristokeides.Api` is purely an ASP.NET Core Web API with JWT and Basic authentication.
- **Architectural Shift**: To implement the UI within the C# ecosystem (D-01), we will integrate Blazor directly into the existing `Aristokeides.Api` project.
  - Add `builder.Services.AddRazorComponents().AddInteractiveServerComponents();` to `Program.cs`.
  - Add `app.MapRazorComponents<App>().AddInteractiveServerRenderMode();` to the request pipeline.
  - Scaffold the standard Blazor Web App structure (`Components` folder, `App.razor`, `Routes.razor`, `MainLayout.razor`, etc.).
- **Authentication Challenge**: Blazor page navigation in a browser relies on cookies, but the app's default auth scheme is JWT.
  - **Action Required**: We must add Cookie Authentication (`.AddCookie()`). We need to configure the authentication schemes so API endpoints accept JWT/Basic, while Blazor routes use Cookies. A simple Blazor login page (or modifying the existing login to issue a cookie) will be required for users to authenticate via the browser.

## 2. Git Operations via LibGit2Sharp
The local repository path pattern is established as `C:/GitRepos/{username}/{repoName}.git`.

- **Branch Tip & Tree Browsing**:
  - Get the latest commit of a branch: `var commit = repo.Branches[branchName].Tip;`
  - Get the root folder: `var tree = commit.Tree;`
  - For nested paths, use the indexer: `var entry = commit[path];`
  - Differentiate types using `entry.TargetType` (`Tree` for folders, `Blob` for files).
- **Commit History & Pagination (D-02)**:
  - Use `repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = branchName, SortBy = CommitSortStrategies.Topological })`.
  - Implement traditional pagination using `.Skip((page - 1) * pageSize).Take(pageSize)`.
- **Reading File Contents**:
  - `var blob = (Blob)entry.Target;`
  - `var content = blob.GetContentText();`

## 3. Syntax Highlighting (D-03)
The decision (D-03) specifies using the simplest possible approach without complex JS integration.
- **Recommended Approach**: Use `highlight.js`.
- **Implementation**:
  1. Include `highlight.js` CDN scripts and a theme CSS in the document head/body (`App.razor`).
  2. Render the file content inside `<pre><code class="language-{ext}">@content</code></pre>`.
  3. Call `IJSRuntime.InvokeVoidAsync("hljs.highlightAll")` in the `OnAfterRenderAsync` lifecycle method of the Blob viewer component.
  This avoids heavy Blazor wrapper libraries and fulfills the requirement for a very lightweight code viewer.

## 4. Routing Structure
Blazor components will be mapped to the following routes as specified in the context:
- `@page "/{username}/{repoName}/tree/{branch}"` (Root directory)
- `@page "/{username}/{repoName}/tree/{branch}/{*path}"` (Subdirectories)
- `@page "/{username}/{repoName}/blob/{branch}/{*path}"` (File viewing)
- `@page "/{username}/{repoName}/commits/{branch}"` (Commit history)

## 5. Security & Authorization
- Blazor pages will use the `[Authorize]` attribute.
- We must inject `AuthenticationStateProvider` to retrieve the current user's ID.
- Before displaying repository data, query `AppDbContext` to verify the repository exists and `repo.OwnerId` matches the current user's ID (replicating the access control logic found in `GitSmartHttpMiddleware`).
