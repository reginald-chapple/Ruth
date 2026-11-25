# AGENTS.md — `Ruth.Persistence`

## Purpose & Boundaries
- Shared PostgreSQL 18 / EF Core 10 persistence infrastructure: abstract `ModuleDbContext`, `AuditAndConcurrencySaveChangesInterceptor`, per-schema transactional `OutboxMessage` / `InboxMessage` pipeline, and PostgreSQL advisory-locked `IScheduledJob` scheduler (`scheduler` schema).
- Contains **no** bounded-context domain aggregates or HTTP/Blazor concerns; depends only on `Ruth.SharedKernel`.

## Durable Invariants
- Validate every dynamic PostgreSQL schema identifier through `SchemaValidator` before interpolating into SQL.
- Preserve `FOR UPDATE SKIP LOCKED` batch claiming and `LockToken` fencing in `OutboxBatchClaimer` / `OutboxStatusUpdater`.
- Claim `(message_id, consumer_name)` atomically via PostgreSQL `INSERT ... ON CONFLICT DO NOTHING` inside a transaction/savepoint **before** executing inner integration-event consumers in `IdempotentIntegrationEventConsumer`, rolling back on failure.
- Serialize `IScheduledJob` execution across hosts via PostgreSQL session advisory locks (`PostgresAdvisoryLockProvider`) and atomic run bookkeeping (`ScheduledJobBookkeeper`).

## Verification
```bash
dotnet build src/Ruth.Persistence/Ruth.Persistence.csproj -warnaserror
dotnet test tests/Ruth.Persistence.Tests/Ruth.Persistence.Tests.csproj
```
