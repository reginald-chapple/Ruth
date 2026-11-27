# AGENTS.md — `Core` Module

## Purpose & Boundaries
- Owns the `core` PostgreSQL schema across two projects: `Ruth.Modules.Core.Contracts` and `Ruth.Modules.Core`.
- `Ruth.Modules.Core.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, or Redis (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.Core.Contracts`.

## Durable Invariants
- Wave 1 (`W1-S3`, `W1-S4`) establishes compile-time modular `EntityTypeDescriptor` discovery (`EntityTypeRegistration` -> `IEntityTypeRegistry`), `DefinedType`/`DefinedValue` aggregates (with GIN-indexed `jsonb` `IHasAttributes` storage on `DefinedValue`), cycle-checked recursive `Category` trees, Redis/in-memory read caches with immediate write invalidation, typed `AttributeDefinition` (global, entity-type, or qualifier-scoped), `AttributeMatrixTemplate` (repeatable structured rows in `jsonb`), and the modular `IFieldTypePlugin` registry (`FrozenDictionary`, 25 foundational FieldTypes).
- Wave 1 (`W1-S5`–`W1-S8`, generalized in `W3-S0`) adds `Branch` (`BranchSchedule`, `BranchTopic`, last-branch deletion guard; representing an organization's Sites, Branches, Chapters, or Centers), cycle-checked `Location` trees with PostGIS `geography(Point, 4326)` and `geography(Polygon, 4326)` GIST indexes and `IAddressVerificationProvider` lock-respecting geocoding, `Device` IP/geofence matching, pure-C# RFC 5545 `Schedule` recurrence with DST/leap-year safety, materialized `ScheduleDate` horizon rows, and `ScheduleCategoryExclusion`, modular `IStorageProvider` (`Ruth.Providers.RustFsStorage` SigV4 streaming/presigned URLs), `BinaryFileType`/`BinaryFile` with `StbImageSharp` crop/resize, `DocumentType`/`Document` quota/temporary lifecycle, cryptographically sealed immutable `SignatureDocument`s, and `HistoryLogEntry` (sensitive masking), threaded `NoteType`/`Note`/`NoteAttachment`/`NoteWatch` (private isolation & non-overridable watch guard), personal/organizational `Tag`/`TaggedItem`, `EntitySet`/`EntitySetItem`, and `PersonPreference`.
- System `DefinedType`s, `DefinedValue`s, `Category` nodes, `AttributeDefinition`s, `Branch`es, `BinaryFileType`s, `BinaryFile`s, `Document`s, and `Tag`s cannot be deleted.
- Category and Location hierarchies must reject self-parenting and ancestor cycles.
- Signed `SignatureDocument` records are immutable once sealed with `SignatureVerificationHash`.
- **Multi-Tenant & RLS Isolation (`W7-S5`, `W7-S6`):** Every table in `core` enforces `ITenantScoped` + PostgreSQL 18 `ENABLE` + `FORCE ROW LEVEL SECURITY` (`ruth_app`), `EntityTypeDescriptor` derives deterministic per-tenant IDs (`CoreTenantSeedContributor`), `CoreReadCache` partitions all local and Redis keys under `t:{TenantId}:core:cache:{bucket}`, and `TenantScopedStorageProvider` prefixes every S3 object key under `tenants/{TenantId}/...`.

## Verification
```bash
dotnet build src/Modules/Core/Ruth.Modules.Core/Ruth.Modules.Core.csproj -warnaserror
dotnet test tests/Ruth.Modules.Core.Tests/Ruth.Modules.Core.Tests.csproj
```
