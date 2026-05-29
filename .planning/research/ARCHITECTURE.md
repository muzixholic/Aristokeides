# ARCHITECTURE.md

## Component Boundaries
1. **Web Interface (UI)**: Handles user interactions, written in Razor Pages/Blazor.
2. **Git HTTP Server**: Middleware to handle `git-receive-pack` and `git-upload-pack` for Git over HTTP.
3. **Core Services**: Domain logic for Issues, PRs, and Auth.
4. **Git Engine Layer**: LibGit2Sharp wrapper for accessing physical repositories on disk.
5. **Data Layer**: EF Core mapping to PostgreSQL.

## Data Flow
- User pushes code -> ASP.NET Core Middleware -> LibGit2Sharp validates -> Written to Disk -> Hook triggers DB update for PRs/Commits.
- User views PR -> UI requests diff -> Git Engine diffs trees -> Results formatted and returned.

## Build Order
1. Auth & Database Foundation
2. Git HTTP Server (Push/Pull)
3. Repository UI & Git Engine Layer
4. Issues & Kanban
5. Pull Requests & Code Review
