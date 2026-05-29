# PITFALLS.md

## Critical Mistakes in Git Hosting
- **Memory Leaks in LibGit2Sharp**: LibGit2Sharp objects are unmanaged. Failing to `Dispose()` Repository objects leads to massive memory leaks.
  - *Prevention*: Always use `using` blocks for `Repository` and related Git objects.
- **Buffering Git Smart HTTP**: Buffering large git pushes in memory (e.g., in ASP.NET Core middleware) will crash the server.
  - *Prevention*: Stream directly from request body to the Git process/LibGit2Sharp.
- **N+1 Queries on Commit History**: Fetching commit authors from the DB in a loop when rendering the commit log.
  - *Prevention*: Batch load or store essential metadata in the Git commit itself.
