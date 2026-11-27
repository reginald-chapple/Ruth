# AGENTS.md — `Ruth.Persistence`

## Purpose & Boundaries
- Shared PostgreSQL 18 / EF Core 10 persistence infrastructure: abstract `ModuleDbContext`, `AuditAndConcurrencySaveChangesInterceptor`, per-schema transactional `OutboxMessage` / `InboxMessage` pipeline, and PostgreSQL advisory-locked `IScheduledJob` scheduler (`scheduler` schema).
- Contains **no** bounded-context domain aggregates or HTTP/Blazor concerns; depends only on `Ruth.SharedKernel`.

## Durable Invariants
- Validate every dynamic PostgreSQL schema identifier through `SchemaValidator` before interpolating into SQL.
- Preserve `FOR UPDATE SKIP LOCKED` batch claiming and `LockToken` fencing in `OutboxBatchClaimer` / `OutboxStatusUpdater`.
- Claim `(message_id, consumer_name)` atomically via PostgreSQL `INSERT ... ON CONFLICT DO NOTHING` inside a transaction/savepoint **before** executing inner integration-event consumers in `IdempotentIntegrationEventConsumer`, rolling back on failure.
- Serialize `IScheduledJob` execution across hosts via PostgreSQL session advisory locks (`PostgresAdvisoryLockProvider`) and atomic run bookkeeping (`ScheduledJobBookkeeper`).
- Enforce multi-tenant defense-in-depth across every tenant `ModuleDbContext` (`IsTenantScopedSchema == true`): separate DDL/control-plane role `ruth_migrator` from non-superuser, non-`BYPASSRLS` DML role `ruth_app` (with explicit privilege revocation on `platform`, `scheduler`, and `__EFMigrationsHistory`), apply EF Core Global Query Filters and annotation-preserving `(tenant_id, ...)` composite unique index scoping on `ITenantScoped`, validate `OriginalValue`/`CurrentValue` and stamp `TenantId` in `AuditAndConcurrencySaveChangesInterceptor`, bind `SELECT set_config('app.current_tenant_id', ...)` per connection/transaction/rollback via `TenantRlsConnectionInterceptor`, `TenantRlsTransactionInterceptor`, and `TenantRlsCommandInterceptor`, enforce `ENABLE` + `FORCE ROW LEVEL SECURITY` on every tenant table, and isolate `OutboxModuleProcessor` and `ScheduledJobExecutor` per tenant with fair-share rotation, per-tenant exception containment, and true `TenantLifecycleStage` propagation.

## Verification
```bash
dotnet build src/Ruth.Persistence/Ruth.Persistence.csproj -warnaserror
dotnet test tests/Ruth.Persistence.Tests/Ruth.Persistence.Tests.csproj
```
