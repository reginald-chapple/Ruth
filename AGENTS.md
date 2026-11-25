# AGENTS.md — Ruth Repository Contract

## 1. Purpose & Solution Boundary

`Ruth` is a clean-architecture Modular Monolith porting Rock RMS (`refs/Rock`, gitignored reference) onto **.NET 10 / C# 14**, **Fluent UI Blazor v5**, **Domain-Driven Design (DDD)**, **PostgreSQL 18** (`postgis`, `pgvector`, `age`), **Redis 8**, and **RustFS (S3)**.

- **Solution file:** `Ruth.slnx`
- **Implementation roadmap & slice catalog:** `docs/IMPLEMENTATION_PLAN.md`
- **Reference source (read-only domain extraction only):** `refs/Rock` — extract business rules and invariants only; never copy `System.Web`, `RockContext`, EF6, `PersonAlias` FK proliferation, or legacy EAV patterns.

## 2. MCAF 1.3 Governance Policies

This repository adopts **MCAF 1.3**:

- **`MCAF-ARCH-001`:** Keep the complete solution in this single repository (`Ruth.slnx`) and use one canonical vertical-slice identity (`W<Wave>-S<Slice>` in planning/PRs and `Slices/<FeatureName>/` across module code, contracts, endpoints, UI, and tests).
- **`MCAF-GOV-001`:** Read root `AGENTS.md` and every affected local `AGENTS.md` completely before editing any of them. Merge incoming governance changes without overwriting, truncating, weakening, or omitting existing durable rules.
- **`MCAF-REQ-001`:** Every non-trivial slice in `docs/IMPLEMENTATION_PLAN.md` maintains explicit scope, acceptance criteria, and automated unit/integration/architecture test coverage (or an explicit documented evidence exception).
- **`MCAF-AI-001`:** Non-trivial decomposition, cross-module contract design, and final integration stay owned by the lead/coordinator agent; delegated subagent or swarm scopes must remain bounded to explicit slices and independently verified before merging.

## 3. Architectural Invariants

1. **Modular Monolith Isolation:**
   - Each Bounded Context under `src/Modules/<Context>/` owns at most two projects: `Ruth.Modules.<Context>.Contracts` and `Ruth.Modules.<Context>`.
   - Modules may only reference `Ruth.SharedKernel` and other modules' `*.Contracts` assemblies—never another module's internal `Domain`, `Infrastructure`, or `Slices`.
   - `*.Domain` namespaces must remain pure C# with zero dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, or Redis.
2. **Zero-Magic CQRS & DDD:**
   - Commands, Queries, Handlers, and API Request/Response DTOs must be `sealed` (`sealed record` / `sealed class`).
   - Use Strongly-Typed `Guid.CreateVersion7()` IDs (`readonly record struct`), `DateTimeOffset` (`timestamptz`), and explicit `Result<T>` domain outcomes instead of throwing exceptions for expected business rule failures.
3. **UI, Templating & API Split:**
   - Internal Staff RMS uses `InteractiveServer` Fluent UI Blazor v5 pages dispatching Application handlers in-process.
   - Public CMS routes use `Static SSR` + `<DynamicComponent>` interactive islands.
   - Templating uses compiled `.razor` components (`HtmlRenderer`) plus safe declarative merge tokens for database-stored content—never Lava/Liquid or arbitrary runtime C# scripting.
   - External HTTP APIs use ASP.NET Core Minimal APIs (`TypedResults`, `CancellationToken` forwarded everywhere, built-in `AddOpenApi()` — never Swashbuckle).

## 4. Tool Ownership & Quality Baseline

Repo-root `.editorconfig` is the single source of truth for per-rule analyzer severities. Bulk switches live in `Directory.Build.props`, and numeric complexity limits live in `CodeMetricsConfig.txt`.

- **Never weaken quality gates:** Never disable `TreatWarningsAsErrors`, downgrade `.editorconfig` error/warning severities, or add unapproved `#pragma warning disable` / `<NoWarn>` to silence build failures. Fix the code instead.
- **C# / MSBuild Formatting Owner (`csharpier`):** `CSharpier` exclusively owns whitespace and layout for `.cs`, `.csproj`, `.props`, `.targets`, and `.slnx`.
- **Non-Whitespace Style & Fixers (`format`):** `dotnet format` is restricted to `style` and `analyzers` subcommands (`--verify-no-changes`), never `whitespace`.
- **Static Analysis (`code-analysis`, `meziantou-analyzer`, `complexity`):** .NET 10 SDK analyzers (`latest-recommended`, Security category = `error`), `Meziantou.Analyzer`, and maintainability rules (`CA1501`, `CA1502`, `CA1505`, `CA1506`) run on every `dotnet build`.
- **Test Runner & Coverage:** `VSTest` with `xunit` (`xunit.analyzers`) and `coverlet.collector` emitting Cobertura XML. Target `CRAP < 15` (`crap-score`) on complex domain services/aggregates.
- **Frontend Assets (`biome`, `stylelint`):** `Biome` (`biome.json`) owns JS/TS/JSON under `src/`; `Stylelint` (`stylelint.config.mjs`) owns `.css`/`.scss` under `src/`.
- **Maintainability Limits:**
  - `file_max_loc`: 400
  - `type_max_loc`: 250
  - `function_max_loc`: 60
  - `max_nesting_depth`: 3
  - `CA1502` method cyclomatic complexity max: 15 (`CodeMetricsConfig.txt`)

## 5. Canonical Verification Commands

Run these commands from the repository root after any code or configuration change:

```bash
# 1. Restore local tools (.NET EF & CSharpier)
dotnet tool restore

# 2. Format check (or `dotnet tool run csharpier format .` when applying fixes)
dotnet tool run csharpier check .

# 3. Build & static analysis (warnings as errors)
dotnet build Ruth.slnx -warnaserror

# 4. Non-whitespace style & analyzer verification
dotnet format style Ruth.slnx --verify-no-changes --no-restore
dotnet format analyzers Ruth.slnx --verify-no-changes --no-restore

# 5. Automated tests + architecture rules + Cobertura coverage
dotnet test Ruth.slnx --no-build --collect:"XPlat Code Coverage" --results-directory artifacts/coverage

# 6. Frontend JS/TS/JSON & CSS gates
npm run lint:js
npm run lint:css
```

## 6. Workspace Skills Routing (`.agents/skills/`)

Route implementation and verification work to the narrowest matching skill in `.agents/skills/`:
- **Governance:** `mcaf`
- **Architecture & DDD:** `architecture`, `modern-csharp`, `blazing-fast-dotnet`
- **Data Access:** `entity-framework-core`
- **Host & HTTP APIs:** `aspnet-core`, `dotnet-webapi`
- **UI & Components:** `blazor`, `fluentui-blazor-usage`
- **Quality, Formatting & Risk:** `quality-ci`, `csharpier`, `format`, `code-analysis`, `analyzer-config`, `meziantou-analyzer`, `complexity`, `crap-score`, `roslynator`, `stylecop-analyzers`
- **Frontend & Browser Audits:** `biome`, `stylelint`, `htmlhint`, `webhint`, `eslint`, `sonarjs`
