# SUMMARY.md

## Key Findings

- **Stack**: ASP.NET Core 9.0, LibGit2Sharp, PostgreSQL (EF Core), Razor/Blazor for lightweight UI.
- **Table Stakes**: Git HTTP Hosting (Smart HTTP), Issues, Pull Requests, Auth.
- **Watch Out For**: Memory leaks with unmanaged LibGit2Sharp objects, buffering large Git requests in memory, N+1 queries when rendering commit logs.

Research indicates that building a Gitea-like platform in .NET is highly feasible with LibGit2Sharp, provided that care is taken around unmanaged memory and stream buffering during Git operations.
