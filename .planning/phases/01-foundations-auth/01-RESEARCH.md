# Phase 1 Research: Foundations & Auth

## Validation Architecture
- **Framework**: xUnit for C# testing.
- **Config**: xUnit test project `Aristokeides.Tests`
- **Commands**: `dotnet test`

## Domain Patterns
- **Entity Framework Core**: Use `IHostedService` or a simple scope during `Program.cs` startup to call `context.Database.MigrateAsync()` ensuring DB is initialized automatically on startup.
- **JWT Auth**: Use `Microsoft.AspNetCore.Authentication.JwtBearer`. Requires configuring TokenValidationParameters in `AddJwtBearer()`.
- **Role Enforcement**: Encode roles directly as `ClaimTypes.Role` inside the JWT token. Use `[Authorize(Roles = "Admin")]` or similar policy-based authorization.

## Dependencies & Gotchas
- **Gotcha**: Using `MigrateAsync()` on startup in a distributed environment can cause race conditions if multiple instances start simultaneously.
  - **Mitigation**: Since this is a single-instance MVP, `MigrateAsync()` on startup is safe and preferred.
- **Gotcha**: JWT token secret must be kept secure and appropriately sized (e.g., 256 bits).

## Security Threat Model
- **T-1-01**: Insecure JWT Secret -> Ensure JWT key is loaded from configuration/secrets, not hardcoded.
- **T-1-02**: Token Replay -> Implement token expiration and potentially refresh tokens.
