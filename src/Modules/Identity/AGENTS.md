# AGENTS.md — `Identity` Module

## Purpose & Boundaries
- Owns the `identity` PostgreSQL schema across two projects: `Ruth.Modules.Identity.Contracts` and `Ruth.Modules.Identity`.
- `Ruth.Modules.Identity.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, or Redis (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.Identity.Contracts`.

## Durable Invariants
- Wave 0 (`W0-S5`) establishes the minimal `identity` schema (`organization_profiles`, `user_accounts`, `security_roles`, `user_roles`, plus `outbox_messages`/`inbox_messages`) to support one-time `/onboarding` and permanent self-registration locking.
- Wave 1 (`W1-S1`, `W1-S2`) adds password lockout tracking, rotating refresh tokens with reuse-detection family revocation, single-use password-reset & account-confirmation tokens, login history, PKCE external OIDC provider sessions/links, custom `SecurityRole` management, ordered hierarchical `ISecured` Allow/Deny `EntityAclRule` evaluation (`AclRuleEvaluator` + Redis-coordinated `FrozenDictionary` snapshot cache + ASP.NET Core `IAuthorizationHandler`), immutable `AuthAuditLog`, SHA-256-hashed `RestApiKey` authentication, and `AuthClient`/`AuthScope` registries.
- Enforce single-organization / single-initial-admin onboarding atomically at the database level (`organization_profiles` singleton key + `CompleteOnboardingCommandHandler` conflict handling), seed default `GlobalDefaultSecured` `Allow` rules for `Administrators`, and invalidate `IOnboardingStateReader` and `IAclSnapshotCache` immediately upon commit.
- Hash passwords exclusively via `IPasswordHashingService`, hash opaque tokens and API keys with SHA-256 before persistence, and reject control/NUL characters and invalid IANA/system time zones in `OnboardingPolicy`.

## Verification
```bash
dotnet build src/Modules/Identity/Ruth.Modules.Identity/Ruth.Modules.Identity.csproj -warnaserror
dotnet test tests/Ruth.Modules.Identity.Tests/Ruth.Modules.Identity.Tests.csproj
```
