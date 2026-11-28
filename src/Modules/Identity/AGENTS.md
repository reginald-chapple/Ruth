# AGENTS.md — `Identity` Module

## Purpose & Boundaries
- Owns the `identity` PostgreSQL schema across two projects: `Ruth.Modules.Identity.Contracts` and `Ruth.Modules.Identity`.
- `Ruth.Modules.Identity.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, or Redis (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.Identity.Contracts`.

## Durable Invariants
- Wave 0 (`W0-S5`) establishes the minimal `identity` schema (`organization_profiles`, `user_accounts`, `security_roles`, `user_roles`, plus `outbox_messages`/`inbox_messages`) to support one-time `/onboarding` and permanent self-registration locking.
- Wave 1 (`W1-S1`, `W1-S2`) adds password lockout tracking, rotating refresh tokens with reuse-detection family revocation, single-use password-reset & account-confirmation tokens, login history, PKCE external OIDC provider sessions/links, custom `SecurityRole` management, ordered hierarchical `ISecured` Allow/Deny `EntityAclRule` evaluation (`AclRuleEvaluator` + Redis-coordinated `FrozenDictionary` snapshot cache + ASP.NET Core `IAuthorizationHandler`), immutable `AuthAuditLog`, SHA-256-hashed `RestApiKey` authentication, and `AuthClient`/`AuthScope` registries.
- Enforce single-initial-admin onboarding atomically per tenant at the database level (`(tenant_id, singleton_key)` composite uniqueness on `organization_profiles` + `CompleteOnboardingCommandHandler` conflict handling), derive collision-free per-tenant deterministic `Administrators` role IDs via `SecurityRole.GetAdministratorsRoleId(tenantId)` (`IdentityTenantSeedContributor`), protect `IsSystem` roles from renaming or deactivation, seed default `GlobalDefaultSecured` `Allow` rules for `Administrators`, and invalidate per-tenant `IOnboardingStateReader` and `IAclSnapshotCache` (`t:{TenantId}:identity:acl:version`) immediately upon commit.
- Enforce `ITenantScoped` EF Core Global Query Filters + PostgreSQL 18 `ENABLE` + `FORCE ROW LEVEL SECURITY` (`ruth_app`) on every table in `identity`, and bind `tid` tenant claims across host-only cookies, JWT access tokens, refresh tokens, and `RestApiKey` lookups.
- Hash passwords exclusively via `IPasswordHashingService`, hash single-use/refresh opaque tokens with SHA-256 and `RestApiKey` secrets with 16-char `ruth_` prefixes + per-key salted PBKDF2-HMAC-SHA256 (`$pbkdf2-s256$100000$<salt>$<hash>`, retaining constant-time SHA-256 verification only for legacy records) before persistence, and reject control/NUL characters and invalid IANA/system time zones in `OnboardingPolicy`.
- **Hardened JWT/OIDC & BOLA Invariants (`W8-S1`, `W8-S3`):** Pin JWT validation strictly to `HS256` (`JwtBearerSecurityEvents` rejecting `alg: none`, key-confusion algorithms, `jku`/`x5u`/`jwk`/`x5c`, traversal `kid`, and cross-tenant/cross-plane `tid` replay); record `RefreshTokenReuseDetected` in `LoginHistoryEntries` when revoking a replayed refresh-token family; route external OIDC discovery/token calls through `SafeOutboundHttpHandler` + `OutboundAddressGuard` while enforcing constant-time `nonce`, PKCE `S256`, `SafeRedirectValidator` local return paths, and RFC 9700 exact ordinal `redirect_uri` matching; and prevent horizontal BOLA on per-user endpoints (`login-history` requires `Administrators` to inspect another `userAccountId`).

## Verification
```bash
dotnet build src/Modules/Identity/Ruth.Modules.Identity/Ruth.Modules.Identity.csproj -warnaserror
dotnet test tests/Ruth.Modules.Identity.Tests/Ruth.Modules.Identity.Tests.csproj
```
