# AGENTS.md — `Crm` Module

## Purpose & Boundaries
- Owns the `crm` PostgreSQL schema across two projects: `Ruth.Modules.Crm.Contracts` and `Ruth.Modules.Crm`, plus the Apache AGE graph `crm_graph`.
- `Ruth.Modules.Crm.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, Redis, or `Ruth.Persistence` (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.Crm.Contracts`. External background-check providers (e.g., `Ruth.Providers.Checkr`) implement `IBackgroundCheckProvider` from `Ruth.Modules.Crm.Contracts`.

## Durable Invariants
- **W2-S1 (`People`):** `Person` aggregate enforces sanitized names, `NickName` fallback to `FirstName`, `AnniversaryDate` cleared when not married, `IsDeceased` derivation, accurate leap-year age/grade calculations, E.164 `PhoneNumber` normalization with SMS opt-out reset on digit change, automatic `AlternateId` search key generation, email change archival to `PersonSearchKey`, Double Metaphone indexing, and PostgreSQL `pg_trgm` + phonetic + nickname search.
- **W2-S2 (`Households` & `Relationships`):** First-class `Household` aggregate supporting blended multi-household memberships, mapped/mailing address promotion to `Previous`, Rock-compatible `GivingLeader` and `Spouse` resolution, reciprocal inverse `KnownRelationship` sync, `PeerNetworkEdge` scoring (growth/decay), and PostgreSQL Apache AGE (`crm_graph`) synchronization for multi-hop graph traversal.
- **W2-S3 (`Duplicates` & `Merge`):** Canonical pair ordering (`Person1Id < Person2Id`), `TotalCapacity = 29` geometric-mean confidence scoring, `IsConfirmedAsNotDuplicate` persistence, `IgnoreUntilScoreChanges` auto-reset on score drift, `PersonDuplicateScannerJob` (`IScheduledJob`), `PersonRedirectLedger` transitive resolution, `AccountProtectionProfile` merge guardrails, SMS consent union, and transactional Outbox `PersonMergedIntegrationEvent` emission.
- **W2-S4 (`Signals`, `Badges`, `Assessments`, `BackgroundChecks`, `Verification`):** `TopSignal` denormalization recalculated on signal/type changes, modular DI-registered `IPersonBadgeEvaluator` bar, assessment retake cooldown & validity rules, HMAC-SHA256-verified Checkr webhook processing with out-of-order status guards, never-serialized `IdentityVerificationCode` with IP daily rate-limiting and max failed attempts, and `PersonViewed` audit logging.

## Verification
```bash
dotnet build src/Modules/Crm/Ruth.Modules.Crm/Ruth.Modules.Crm.csproj -warnaserror
dotnet test tests/Ruth.Modules.Crm.Tests/Ruth.Modules.Crm.Tests.csproj
```
