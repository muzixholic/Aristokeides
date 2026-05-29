# STACK.md

## Standard Stack for Git Management System (C# / .NET)

- **Framework**: ASP.NET Core 9.0 (High performance web API and UI)
- **Git Operations**: LibGit2Sharp (Native Git implementation for .NET, much faster than wrapping git.exe)
- **Database**: PostgreSQL with Entity Framework Core (Standard for Git tools like GitLab, robust JSON support)
- **Frontend**: Razor Pages or Blazor Server (Lightweight, fits the .NET ecosystem well without SPA complexity)
- **Auth**: ASP.NET Core Identity (JWT or Cookie based)

## What NOT to use
- **Raw `git.exe` calls via Process.Start**: Slow, vulnerable to injection, hard to scale. Use LibGit2Sharp instead.
- **Heavy SPA frameworks (React/Angular)**: Unless explicitly required, adds unnecessary complexity for an MVP that aims to be lightweight like Gitea.
