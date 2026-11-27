# AGENTS.md — `Crm` Module

## Purpose & Boundaries
- Owns the `crm` PostgreSQL schema across two projects: `Ruth.Modules.Crm.Contracts` and `Ruth.Modules.Crm`.
- `Ruth.Modules.Crm.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, Redis, or `Ruth.Persistence` (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.Crm.Contracts`. External background-check providers (e.g., `Ruth.Providers.Checkr`) implement `IBackgroundCheckProvider` from `Ruth.Modules.Crm.Contracts`.

## Durable Invariants
- **W2-S1 (`People`):** `Person` aggregate enforces sanitized names, `NickName` fallback to `FirstName`, `AnniversaryDate` cleared when not married, `IsDeceased` derivation, accurate leap-year age/grade calculations, E.164 `PhoneNumber` normalization with SMS opt-out reset on digit change, automatic `AlternateId` search key generation, email change archival to `PersonSearchKey`, Double Metaphone indexing, and PostgreSQL `pg_trgm` + phonetic + nickname search.
- **W2-S2 (`Households` & `Relationships`, generalized in `W3-S0`, relationalized in `W7-S1`):** First-class `Household` aggregate supporting blended multi-household memberships, `BranchId` affiliation, mapped/mailing address promotion to `Previous`, gender-neutral `IsPrimaryContact`-first primary/billing contact and married-partner resolution, reciprocal inverse `KnownRelationship` sync, clean-room bounded-growth & exponential half-life decay `PeerNetworkEdge` scoring, and RLS-protected relational multi-hop graph traversal (`RelationalCrmGraphService`).
- **W2-S3 (`Duplicates` & `Merge`, generalized in `W3-S0`):** Canonical pair ordering (`Person1Id < Person2Id`), clean-room completeness-adjusted weighted attribute similarity scoring (`MaxPossibleWeight = 100`, with suffix collision penalty for `Jr.`/`Sr.`), `IsConfirmedAsNotDuplicate` persistence, `IgnoreUntilScoreChanges` auto-reset on score drift, `PersonDuplicateScannerJob` (`IScheduledJob`), `PersonRedirect` (`ExternalRecordGuid`) transitive resolution, `AccountProtectionProfile` merge guardrails, SMS consent union, and transactional Outbox `PersonMergedIntegrationEvent` emission.
- **W2-S4 (`Signals`, `Badges`, `Assessments`, `BackgroundChecks`, `Verification`):** `TopSignal` denormalization recalculated on signal/type changes, modular DI-registered `IPersonBadgeEvaluator` bar, assessment retake cooldown & validity rules, HMAC-SHA256-verified Checkr webhook processing with out-of-order status guards, never-serialized `IdentityVerificationCode` with IP daily rate-limiting and max failed attempts, and `PersonViewed` audit logging.
- **Multi-Tenant & RLS Isolation (`W7-S5`):** Every table in `crm` enforces `ITenantScoped` + PostgreSQL 18 `ENABLE` + `FORCE ROW LEVEL SECURITY` (`ruth_app`), composite `(tenant_id, ...)` uniqueness on `MetaphoneEntry` and `IdentityVerificationCode`, and deterministic per-tenant reference seeding via `CrmTenantSeedContributor`.

## Verification
```bash
dotnet build src/Modules/Crm/Ruth.Modules.Crm/Ruth.Modules.Crm.csproj -warnaserror
dotnet test tests/Ruth.Modules.Crm.Tests/Ruth.Modules.Crm.Tests.csproj
```
