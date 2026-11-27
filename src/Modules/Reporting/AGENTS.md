# AGENTS.md — `Reporting` Module

## Purpose & Boundaries
- Owns the `reporting` PostgreSQL schema across two projects: `Ruth.Modules.Reporting.Contracts` and `Ruth.Modules.Reporting`.
- `Ruth.Modules.Reporting.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, Redis, or `Ruth.Persistence` (enforced by `Ruth.ArchitectureTests`).
- `Ruth.Modules.Reporting.Contracts` may only depend on `Ruth.SharedKernel` (pure C# contracts including `IDataViewSourceProvider` and `IFieldSelectPlugin`).

## Durable Invariants
- **W6-S5 (`DataView`, `DataViewFilter`, `DataViewPersistedValue`, `Report`, `ReportField`, `MergeTemplate`):**
  - Declarative Query AST (`GroupAll` AND, `GroupAny` OR, `GroupNone` NOR, `GroupAllFalse` NAND, plus `EntityProperty`, `JsonbAttribute`, `SpatialRadius`/`SpatialPolygon`, and `CrossModulePersonSet` rules) enforces depth/node bounds (`DataViewAstValidator`) and compiles exclusively against allowlisted columns and parameterized predicates (`DataViewSqlCompiler`).
  - `DataViewMaterializerJob` refreshes stale persisted entity sets on schedule; `ReportingPersonMergedConsumer` idempotently repoints retired `PersonId`s in `DataViewPersistedValue` and `MergeTemplate.OwnerPersonId`.
  - `Report` projects rows through DI-registered `IFieldSelectPlugin` strategies and exports RFC 4180 CSV (with formula-injection prefixing) and clean-room OpenXML `.xlsx` workbooks (`ReportingDocumentPackager`).
  - `MergeTemplate` renders HTML and clean-room OpenXML `.docx` documents via `IMergeTemplateEngine` without arbitrary runtime scripting.
- **W6-S6 (`MetricCategory`, `Metric`, `MetricPartition`, `MetricValue`, Executive Dashboards, BI Star-Schema):**
  - `MetricValue` validates required `MetricPartition` entity assignments; `MetricCalculationJob` executes DataView-backed and allowlisted SQL metrics.
  - `MetricChartRollupCalculator` computes `Day`/`Week`/`Month`/`Quarter` rollups, 3-bucket rolling averages, and period deltas.
  - `StarSchemaProjectionRefresher` + `StarSchemaRefreshJob` materialize `dim_date`, `dim_branch`, `dim_metric`, `fact_metric_daily`, `fact_revenue_monthly`, `fact_attendance_weekly`, and BI views (`vw_bi_metric_facts`, `vw_bi_revenue_summary`, `vw_bi_attendance_summary`).
- **Multi-Tenant & RLS Isolation (`W7-S6`):** Every table in `reporting` enforces `ITenantScoped` + PostgreSQL 18 `ENABLE` + `FORCE ROW LEVEL SECURITY` (`ruth_app`), and `DataView` SQL execution and Star-Schema views (`security_invoker = true` + `app.current_tenant_id` predicate) isolate rows strictly per tenant.

## Verification
```bash
dotnet build src/Modules/Reporting/Ruth.Modules.Reporting/Ruth.Modules.Reporting.csproj -warnaserror
dotnet test tests/Ruth.Modules.Reporting.Tests/Ruth.Modules.Reporting.Tests.csproj
```
