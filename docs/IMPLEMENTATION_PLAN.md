# Ruth — Relationship, Community & Operations Platform Implementation Plan

> **Mission:** Build a clean-room, high-performance **.NET 10 + Fluent UI Blazor v5 + Domain-Driven Design (DDD)** Modular Monolith serving **any member-driven, community, non-profit, or service organization** (handling both tax-deductible funds/grants and standard dues, fees, and invoices), backed by **PostgreSQL 18**, **Redis 8**, and **RustFS (S3)**.

---

## 1. Locked Architectural Decisions

| # | Decision Area | Locked Choice | Architectural Consequence |
| :--- | :--- | :--- | :--- |
| **1** | **Clean-Room & Domain Posture** | **First-Principles & Organization-Agnostic** | Clean PostgreSQL-native DDD schema (`uuidv7` Strongly-Typed IDs, `timestamptz` / `DateTimeOffset`, `jsonb` + GIN indexes, `postgis`, `pgvector`, Apache `age`). Zero code, proprietary heuristics, or schema copying from restrictively licensed ChMS/RMS systems. Operating units are modeled neutrally as `Branch` (`Site` / `Branch` / `Chapter` / `Center`); financial ledgers support both tax-deductible funds and standard dues/fees/invoices. Zero sample data on boot; `/onboarding` locks self-registration after initial admin setup. |
| **2** | **UI Paradigm** | **Option A — Hybrid App Shell + CMS Engine** | **Internal Staff Workspace:** Fast, idiomatic Fluent UI Blazor v5 routed pages (`/crm/people/{id}`, `/finance/batches`, `/groups/{id}`, `/admin/core/branches`) with optional customizable widget zones.<br>**External / Portals / Custom Pages:** Full database-driven `Site -> Layout -> Page -> Zone -> Block` engine rendered via Blazor `<DynamicComponent>` (`IRuthBlock`). |
| **3** | **Blazor Render Modes & APIs** | **`InteractiveServer` (Staff) + `Static SSR` Islands (CMS) + Minimal APIs** | Staff components invoke Application-layer `ICommandHandler`/`IQueryHandler` in-process (zero redundant HTTP DTO client wrappers for internal views). Public CMS uses `Static SSR` + interactive islands. Check-in Kiosk uses a resilient low-latency interactive flow. External/mobile/webhook consumers use ASP.NET Core Minimal APIs + built-in OpenAPI (`AddOpenApi()`). |
| **4** | **CQRS, Messaging & Jobs** | **Zero-Magic Explicit Abstractions** | Custom lightweight `ICommand<T>`, `ICommandHandler<TCommand, T>`, `IQuery<T>`, `IQueryHandler<TQuery, T>`, `IDomainEvent`, `IIntegrationEvent`, and `Result<T>` in `Ruth.SharedKernel` (zero commercial licensing risk from MediatR/MassTransit, zero reflection magic). Per-module EF Core **Transactional Outbox** + `BackgroundService` dispatcher + Postgres-backed `IScheduledJob` runner. |
| **5** | **Extensibility Model** | **Modular Assemblies First** | Clean DI-registered strategy contracts (`IPaymentGateway`, `ICommunicationTransport`, `IStorageProvider`, `IWalletPassProvider`, `IVirtualRoomProvider`, `IFieldTypePlugin`, `IWorkflowAction`, `IBackgroundCheckProvider`, `IAiProvider`) compiled as modular assemblies, preserving clean seams if dynamic runtime loading is ever desired later. |
| **6** | **Templating & Rendering** | **Compiled Razor Components (`HtmlRenderer`) + Safe Merge Tokens** | Built-in layouts, transactional emails, annual tax receipts, invoices, and CMS blocks are strongly-typed `.razor` components rendered via Blazor's built-in `HtmlRenderer`. Database-stored, staff-editable content (bulk emails, SMS, waivers, merge fields) uses a safe declarative merge-token engine (`IMergeTemplateEngine` / `TemplateSnippet`) over read-only DTO contexts—eliminating arbitrary runtime scripting/RCE risk. |

---

## 2. Target Solution & Bounded-Context Layout

To satisfy `.agents/skills/architecture` (*"avoid creating extra projects that add ceremony without ownership clarity"*) and `.agents/skills/mcaf` (`MCAF-ARCH-001` canonical vertical-slice identity), each Bounded Context consists of **at most two projects** (`Contracts` + core module containing `Domain`, `Infrastructure`, and `Slices`), with inward dependency rules enforced by `Ruth.ArchitectureTests`.

```text
Ruth.slnx
AGENTS.md                                  # Root repository contract & runnable verification commands
Directory.Build.props                      # net10.0, LangVersion=14, Nullable=enable, TreatWarningsAsErrors=true
Directory.Build.targets
.editorconfig                              # Single source of truth for analyzer severities
CodeMetricsConfig.txt                      # CA1502/CA1505/CA1506 maintainability & complexity thresholds
src/
  Ruth.SharedKernel/                       # AggregateRoot, Entity, ValueObject, StronglyTypedId, Result<T>, IClock, Zero-Magic CQRS & Outbox primitives
  Ruth.Persistence/                        # ModuleDbContext, Transactional Outbox/Inbox, Postgres Advisory-Locked Scheduler
  Ruth.Host/                               # ASP.NET Core 10 + Fluent UI Blazor v5 App Shell, /onboarding, Middleware, Composition Root
  Modules/
    Identity/                              # Schema: identity  (Users, Logins, OIDC/JWT, SecurityRoles, Hierarchical ISecured ACLs)
      Ruth.Modules.Identity.Contracts/
      Ruth.Modules.Identity/               # Domain/, Infrastructure/, Slices/<SliceName>/
    Core/                                  # Schema: core      (DefinedTypes, Categories, jsonb Attributes/FieldTypes, Branches/Sites, PostGIS Locations, Schedules, BinaryFiles, Audit, Notes, Tags)
    Crm/                                   # Schema: crm       (People/Organizations, Households/Units, Apache AGE Relationships, Merge/Deduplication, Badges, Assessments, Screening)
    Groups/                                # Schema: groups    (GroupTypes, Groups/Chapters/Teams, Memberships, Requirements, Volunteer & Shift Scheduler, Attendance)
    CheckIn/                               # Schema: checkin   (Kiosk & Facility Check-In Config, Household/Member Matching, Capacity Thresholds, ZPL Badges, Smart Apple/Google Wallet Passes)
    Communication/                         # Schema: comms     (Razor HtmlRenderer + Safe Merge Engine, Transports, Templates, Bulk Email/SMS/Push, Two-Way SMS, Drip Flows)
    Finance/                               # Schema: finance   (Tax-Deductible Funds & Revenue Accounts, Gateways, Batches, Donations/Dues/Fees/Invoices, Recurring Billing, Statements/Receipts, Assistance/Grants, Campaigns)
    Events/                                # Schema: events    (Calendars, EventItems, Registrations, Tiered Fees/Discounts/Waitlists, Live Interactive Sessions)
    Commerce/                              # Schema: commerce  (Products/Merch/Digital Goods, Punch/Credit Packs, Member Pricing, Quid-Pro-Quo Splits, 1-on-1 Service Appointments & Facility/Resource Rentals)
    Engagement/                            # Schema: engage    (Engagement Pipelines, Journeys/Milestones, Streaks, Achievements, Care/Support Requests, Reminders, Following, Touchpoints)
    Workflow/                              # Schema: workflow  (WorkflowTypes, State/Activity/Action Engine, Triggers, Interactive Form Builder)
    VirtualRooms/                          # Schema: virtual   (Embedded LiveKit WebRTC Rooms, ACL Room JWTs, Webhook Auto-Attendance & RustFS Recording Egress)
    Cms/                                   # Schema: cms       (Sites, Layouts, Pages, Zones, Dynamic Blocks, Content Channels, Shortlinks, Media, Interactions, Personalization, Mobile/TV)
    Reporting/                             # Schema: reporting (Query AST DataViews, Reports, MergeTemplates, Operational & Financial Metrics, Star-Schema Projections)
    Lms/                                   # Schema: lms       (Programs, Courses, Terms, Classes, Activities, Grading & Certifications)
    Ai/                                    # Schema: ai        (Content Collections, pgvector Hybrid Search, AI Providers, Agents & Skills/Tools)
  Providers/                               # Modular provider assemblies (RustFsStorage, Smtp, SendGrid, Mailgun, Nmi, Stripe, Checkr, AppleWallet, GoogleWallet, LiveKit, OpenAi, etc.)
tests/
  Ruth.ArchitectureTests/                  # Enforces module isolation, Domain purity, sealed handlers/DTOs
  Ruth.Modules.<Context>.Tests/            # Unit + Testcontainers (PostgreSQL 18 / Redis 8 / RustFS) slice tests
```

---

## 3. `.agents/skills` Catalog & Non-Overlapping Ownership Matrix

Every skill in `.agents/skills/` has a distinct role. Per `quality-ci`, **overlapping formatters and linters must never fight over the same files**.

### 3.1 Architecture, Backend & UI Authoring Skills

| Skill | Path | Primary Responsibility & Trigger Scope |
| :--- | :--- | :--- |
| **`mcaf`** | [.agents/skills/mcaf/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/mcaf/SKILL.md) | Root and per-module `AGENTS.md` governance, canonical slice naming (`MCAF-ARCH-001`), requirement-to-test traceability (`MCAF-REQ-001`), bounded agent delegation (`MCAF-AI-001`). Only invoke when establishing or updating governance contracts. |
| **`architecture`** | [.agents/skills/architecture/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/architecture/SKILL.md) | Modular Monolith boundaries, DDD Aggregates/Entities/Value Objects, Domain vs. Integration Events, Zero-Magic CQRS command/query splits, and vertical slice organization without over-abstraction. |
| **`modern-csharp`** | [.agents/skills/modern-csharp/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/modern-csharp/SKILL.md) | Idiomatic C# 14 on `.NET 10`: `sealed` classes/records, primary constructors, `readonly record struct` Strongly-Typed IDs, pattern matching, collection expressions, `System.Threading.Lock`. |
| **`aspnet-core`** | [.agents/skills/aspnet-core/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/aspnet-core/SKILL.md) | `Ruth.Host` request pipeline & middleware ordering, `IOptions<T>` configuration, cookie/JWT/OIDC authentication, `IAuthorizationHandler` policies, `IHttpClientFactory`, `BackgroundService` workers, health checks. |
| **`dotnet-webapi`** | [.agents/skills/dotnet-webapi/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/dotnet-webapi/SKILL.md) | Minimal API resource endpoints (`Map<Resource>`), `sealed record` Request/Response DTOs with XML `<summary>` docs, `DateTimeOffset`, string enums, `TypedResults`, built-in `AddOpenApi()` (never Swashbuckle), RFC 7807 `ProblemDetails`, and `.http` test files. |
| **`entity-framework-core`** | [.agents/skills/entity-framework-core/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/entity-framework-core/SKILL.md) | Per-module `DbContext` & PostgreSQL schema isolation, `IEntityTypeConfiguration<T>`, `jsonb`/`postgis`/`pgvector` mappings, `AsNoTracking()` DTO projections, compiled hot queries, concurrency tokens, reviewable migrations. |
| **`blazor`** | [.agents/skills/blazor/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/blazor/SKILL.md) | `.NET 10` render modes (`InteractiveServer` for Staff UI, `Static SSR` + islands for CMS), `[PersistentState]`, `<ErrorBoundary>`, `<Virtualize>`, `@key` diffing, debounced search, async `IJSRuntime` interop. |
| **`fluentui-blazor-usage`** | [.agents/skills/fluentui-blazor-usage/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/fluentui-blazor-usage/SKILL.md) | `Microsoft.FluentUI.AspNetCore.Components` **v5** patterns: `<FluentProviders />`, JS/CSS custom-property theming, `FluentLayout`/`FluentNav`/`FluentNavCategory`, `FluentDataGrid` (`ItemsProvider`), two-parameter `FluentSelect<TOption, TValue>`, `IDialogService` (`IDialogInstance` cascading parameter). |
| **`blazing-fast-dotnet`** | [.agents/skills/blazing-fast-dotnet/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/blazing-fast-dotnet/SKILL.md) | Hot-path engineering: `[LoggerMessage]` source-gen logging, STJ `JsonSerializerContext`, `[GeneratedRegex]`, `FrozenDictionary` caches (ACLs, SystemGuids, FieldTypes), `Span<T>`/`SearchValues`, `StringComparison.Ordinal`, `dotnet-counters`/`dotnet-trace` profiling. |

### 3.2 .NET Quality, Formatting, Static Analysis & Risk Gates

| Skill | Path | Ownership Rule (Non-Overlapping) |
| :--- | :--- | :--- |
| **`quality-ci`** | [.agents/skills/quality-ci/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/quality-ci/SKILL.md) | Orchestrates the single canonical CI & post-slice verification pass (format -> build/analyze -> tests -> architecture -> coverage). |
| **`csharpier`** | [.agents/skills/csharpier/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/csharpier/SKILL.md) | **Exclusive whitespace & layout owner** for `.cs`, `.csproj`, `.props`, `.targets`, and `.slnx` via `dotnet csharpier check .`. |
| **`format`** | [.agents/skills/format/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/format/SKILL.md) | Scoped **strictly to `dotnet format style` and `dotnet format analyzers` (`--verify-no-changes`)**—never `whitespace`, so it never conflicts with `csharpier`. |
| **`code-analysis`** | [.agents/skills/code-analysis/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/code-analysis/SKILL.md) | First-party .NET 10 SDK analyzers (`EnableNETAnalyzers=true`, `AnalysisLevel=latest-recommended`, `TreatWarningsAsErrors=true`, Security `CA3xxx`/`CA5xxx = error`). Agents must **never** weaken warning gates. |
| **`analyzer-config`** | [.agents/skills/analyzer-config/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/analyzer-config/SKILL.md) | Owns the repo-root `.editorconfig` rule severity layout (`root = true`, no inline comments). |
| **`meziantou-analyzer`** | [.agents/skills/meziantou-analyzer/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/meziantou-analyzer/SKILL.md) | **Primary third-party Roslyn analyzer pack** (`Meziantou.Analyzer`) catching async/CancellationToken misuse, culture-unsafe string ops, concurrency bugs, and security issues. |
| **`complexity`** | [.agents/skills/complexity/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/complexity/SKILL.md) | Enforces `CA1502` (cyclomatic complexity), `CA1505` (maintainability), `CA1506` (class coupling), and `CA1501` via checked-in `CodeMetricsConfig.txt`. |
| **`crap-score`** | [.agents/skills/crap-score/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/crap-score/SKILL.md) | Evaluates Change Risk Anti-Patterns (`CRAP < 15` target, never `> 30`) using real Cobertura coverage on complex domain services/engines (ACLs, Merge, Check-In, Template/Merge Engine, DataViews, Batches/Fees, Workflows). |
| **`roslynator`** | [.agents/skills/roslynator/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/roslynator/SKILL.md) | **On-demand CLI cleanup & dead-code audit** (`roslynator find-unused`) at the end of each Wave; not stacked as duplicate build errors over SDK + Meziantou. |
| **`stylecop-analyzers`** | [.agents/skills/stylecop-analyzers/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/stylecop-analyzers/SKILL.md) | **Scoped opt-in only** if public `Contracts`/API DTO XML documentation rules (`SA1600`) are desired; layout rules stay disabled so `csharpier` remains sole layout owner. |

### 3.3 Frontend Asset, Stylesheet & Runtime Browser Audit Skills

| Skill | Path | Ownership Rule (Non-Overlapping) |
| :--- | :--- | :--- |
| **`biome`** | [.agents/skills/biome/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/biome/SKILL.md) | **Primary formatter & linter** (`biome.json`) for Blazor JS/TS interop modules (`wwwroot/**/*.js`, `*.ts`) and static JSON assets. |
| **`stylelint`** | [.agents/skills/stylelint/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/stylelint/SKILL.md) | **Stylesheet owner** for custom CSS (`wwwroot/**/*.css`, `.razor.css` isolation styles, Fluent v5 token usage, and CMS themes). |
| **`htmlhint`** | [.agents/skills/htmlhint/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/htmlhint/SKILL.md) | Validates **rendered/static HTML output** (exported Email HTML templates, Tax Receipt / Invoice HTML, and rendered CMS pages)—never raw `.razor` source files. |
| **`webhint`** | [.agents/skills/webhint/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/webhint/SKILL.md) | **Runtime URL auditor** (`.hintrc`) checking accessibility (a11y), security headers, performance, PWA signals, and SEO against live `/onboarding`, Staff Shell, Check-in Kiosk, and Public CMS routes. |
| **`eslint`** | [.agents/skills/eslint/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/eslint/SKILL.md) | Standby (`not_applicable` by default while `biome` owns JS/TS); activate **only** on bounded rich client packages (e.g., visual Form/Workflow canvas) if specialized ESLint plugins are required. |
| **`sonarjs`** | [.agents/skills/sonarjs/SKILL.md](file:///Users/reginaldchapple/Developer/Ruth/.agents/skills/sonarjs/SKILL.md) | Cognitive-complexity extension (`eslint-plugin-sonarjs`) paired with `eslint` **only** if complex client-side TypeScript editors (ZPL label canvas / Workflow designer) are introduced. |

---

## 4. Standard Slice Execution Contract ("Clean-Room & Shippable")

Every slice (`W<Wave>-S<Slice>`) is an atomic, shippable vertical increment. **Never start N+1 infrastructure for a future wave inside an earlier slice.**

```mermaid
flowchart LR
    A["1. First-Principles Spec<br/>(Clean-Room Invariants)"] --> B["2. Domain Model<br/>(architecture, modern-csharp)"]
    B --> C["3. EF Core + Migration<br/>(entity-framework-core)"]
    C --> D["4. Zero-Magic CQRS Handlers<br/>(architecture, blazing-fast-dotnet)"]
    D --> E["5. UI & Minimal APIs<br/>(blazor, fluentui-blazor-usage, dotnet-webapi)"]
    E --> F["6. Quality & CRAP Gate<br/>(csharpier, code-analysis, complexity, crap-score)"]
```

### Definition of Done (Every Slice)
1. **Clean-Room Specification:** Define domain invariants, edge cases, and open-standard workflows from first principles—never referencing or copying restrictively licensed third-party code or proprietary formulas.
2. **Domain Purity:** Aggregates enforce invariants via methods returning `Result<T>`, emit `IDomainEvent`s, use `Guid.CreateVersion7()` Strongly-Typed IDs, remain organization-agnostic, and have **zero** dependencies on EF Core, ASP.NET Core, or Blazor.
3. **Persistence:** Module-scoped `DbContext` + explicit `IEntityTypeConfiguration<T>` + reviewable EF Core migration + `AsNoTracking()` read projections.
4. **Presentation:** Fluent UI Blazor v5 pages/dialogs (with `<ErrorBoundary>`, loading states, and ACL checks) and/or Minimal API endpoints (`TypedResults` + OpenAPI metadata + `.http` requests).
5. **Automated Tests:** Domain unit tests + Testcontainers PostgreSQL 18 integration tests + `Ruth.ArchitectureTests` green.
6. **Post-Slice Quality Gate:**
   - `dotnet tool run csharpier check .` (`csharpier`)
   - `dotnet build Ruth.slnx -warnaserror` (`code-analysis`, `meziantou-analyzer`, `complexity`)
   - `dotnet format style Ruth.slnx --verify-no-changes && dotnet format analyzers Ruth.slnx --verify-no-changes` (`format`)
   - `dotnet test` with Cobertura coverage + `crap-score` check (`CRAP < 15`) on newly added complex domain methods.

---

## 5. Wave Dependency Roadmap

```mermaid
flowchart TD
    W0["Wave 0: Platform, Governance & Shared Kernel (5 Slices)"] --> W1["Wave 1: Identity, Security & Core Foundation (8 Slices)"]
    W1 --> W2["Wave 2: Constituent CRM & Groups/Teams (8 Slices)"]
    W2 --> W3["Wave 3: Generalization, Check-In, Comms & Wallet Passes (9 Slices)"]
    W2 --> W4["Wave 4: Finance, Events & Commerce/Bookings (10 Slices)"]
    W2 --> W5["Wave 5: Engagement, Care, Workflow & LiveKit Rooms (7 Slices)"]
    W3 --> W6["Wave 6: CMS, Reporting, LMS & AI (8 Slices)"]
    W4 --> W6
    W5 --> W6
    W6 --> W7["Wave 7 (Optional): Universal CSV/JSON Data Import Pipeline (1 Slice)"]
```

> [!TIP]
> With **Wave 2** and **`W3-S0` (Clean-Room & Organization-Agnostic Generalization)** complete, **Waves 3, 4, and 5** depend primarily on `Identity`, `Core`, `Crm.Contracts`, and `Groups.Contracts`—meaning slices across Check-In/Comms, Finance/Events, and Engagement/Workflow can be executed **in parallel** (e.g., via `/teamwork` swarm workers in isolated workspaces).

---

## 6. Detailed Wave & Vertical Slice Catalog

### Wave 0 — Platform, Governance & Shared Kernel
*Goal: Establish repository governance, zero-warning CI gates, DDD/CQRS building blocks, Docker infrastructure, and the Fluent UI v5 host shell with zero-seed `/onboarding`.*

#### `W0-S1` — Repository Governance, Solution Skeleton & Quality Gates ✅ *(Completed)*
* **Scope:** Configure root `AGENTS.md`, `Ruth.slnx`, `Directory.Build.props` (`net10.0`, `LangVersion=14`, `TreatWarningsAsErrors=true`, `EnableNETAnalyzers=true`, `Meziantou.Analyzer`), repo-root `.editorconfig`, `CodeMetricsConfig.txt`, local `csharpier` tool manifest, `biome.json`, `stylelint.config.mjs`, Docker stack ([Dockerfile](file:///Users/reginaldchapple/Developer/Ruth/deploy/docker/dotnet/Dockerfile), `docker-compose.yml`), and `tests/Ruth.ArchitectureTests`.
* **Authoring Skills:** `mcaf`, `architecture`, `quality-ci`, `analyzer-config`, `code-analysis`, `meziantou-analyzer`, `complexity`, `csharpier`, `biome`, `stylelint`.
* **Verification Skills:** `quality-ci`, `csharpier`, `format`, `code-analysis`.

#### `W0-S2` — `Ruth.SharedKernel` (DDD & Zero-Magic CQRS Primitives) ✅ *(Completed)*
* **Scope:** `Entity<TId>`, `AggregateRoot<TId>`, `IStronglyTypedId<T>` (`Guid.CreateVersion7()`), `ValueObject`, `Result` / `Result<T>` / `Error`, `IClock` (`TimeProvider` adapter), `IDomainEvent` / `IDomainEventHandler<T>`, `IIntegrationEvent` / `IIntegrationEventHandler<T>`, explicit DI-dispatched `ICommand<TResponse>` / `ICommandHandler<TCommand, TResponse>` and `IQuery<TResponse>` / `IQueryHandler<TQuery, TResponse>` with logging/validation decorator pipeline.
* **Authoring Skills:** `architecture`, `modern-csharp`, `blazing-fast-dotnet`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W0-S3` — Module Persistence Base, Transactional Outbox & Scheduled Job Engine ✅ *(Completed)*
* **Scope:** Abstract `ModuleDbContext` (Npgsql 18 with `postgis`, `pgvector`, `age` readiness), audit timestamp/concurrency `SaveChangesInterceptor`, per-schema `OutboxMessage` + `InboxMessage` tables, `BackgroundService` Outbox dispatcher, and Postgres-advisory-locked cron `IScheduledJob` engine.
* **Authoring Skills:** `entity-framework-core`, `architecture`, `aspnet-core`, `modern-csharp`, `blazing-fast-dotnet`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W0-S4` — `Ruth.Host` Pipeline, Observability & Fluent UI Blazor v5 App Shell ✅ *(Completed)*
* **Scope:** `Program.cs` middleware pipeline (`ApiExceptionHandler` -> HTTPS -> `MapStaticAssets` -> Routing -> Auth -> RateLimiter -> OutputCache -> Endpoints), built-in `AddOpenApi()`, `/health` checks (Postgres, Redis, RustFS), OpenTelemetry logging/metrics/tracing, `<FluentProviders />`, `MainLayout.razor` (`FluentLayout`, `FluentNav`, light/dark theme toggle via `Blazor.theme`, `<ErrorBoundary>`).
* **Authoring Skills:** `aspnet-core`, `blazor`, `fluentui-blazor-usage`, `dotnet-webapi`, `stylelint`, `biome`.
* **Verification Skills:** `csharpier`, `code-analysis`, `webhint` (against local host shell).

#### `W0-S5` — Zero-Seed First-Run `/onboarding` & Production Startup Guards ✅ *(Completed)*
* **Scope:** Production fail-fast validator if `Authentication:Jwt:SigningKey` uses dev default; onboarding state gate middleware redirecting all routes to `/onboarding` when zero admin users exist; one-time Administrator + Organization & Primary Branch/Site setup wizard that permanently locks self-registration upon completion (matching [.env.example](file:///Users/reginaldchapple/Developer/Ruth/.env.example)).
* **Authoring Skills:** `aspnet-core`, `blazor`, `fluentui-blazor-usage`, `entity-framework-core`, `modern-csharp`.
* **Verification Skills:** `csharpier`, `code-analysis`, `webhint`, `crap-score`.

---

### Wave 1 — Identity, Security & Core Foundation
*Goal: Deliver enterprise authentication, hierarchical Allow/Deny entity security, extensible `jsonb` attributes/FieldTypes, Branches/Sites, PostGIS locations, iCal schedules, and RustFS storage.*

#### `W1-S1` — Authentication, User Logins & OIDC/JWT Providers ✅ *(Completed)*
* **Scope:** `Identity` module (`identity` schema): `UserAccount`, Argon2id/ASP.NET Core password hashing, Cookie auth for Blazor Server, JWT Bearer + Refresh Tokens for APIs/Mobile, external OIDC provider abstraction, password reset & account confirmation flows, login history, `Auth.http`.
* **Authoring Skills:** `aspnet-core`, `architecture`, `entity-framework-core`, `dotnet-webapi`, `blazor`, `fluentui-blazor-usage`, `modern-csharp`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W1-S2` — Security Roles & Hierarchical `ISecured` ACL Engine ✅ *(Completed)*
* **Scope:** `SecurityRole`, special roles (`AllUsers`, `AllAuthenticatedUsers`), `EntityAclRule` (ordered `Allow`/`Deny` per action `View`, `Edit`, `Administrate`, etc. with parent-entity hierarchy inheritance), `AuthAuditLog`, `RestApiKey`, `AuthClient`/`AuthScope`, Redis-backed `FrozenDictionary` ACL evaluator + ASP.NET Core `IAuthorizationHandler` + reusable Blazor `<SecurityDialog />`.
* **Authoring Skills:** `architecture`, `aspnet-core`, `blazing-fast-dotnet`, `entity-framework-core`, `fluentui-blazor-usage`, `blazor`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W1-S3` — Entity Registry, DefinedTypes & Hierarchical Categories ✅ *(Completed)*
* **Scope:** `Core` module (`core` schema): `EntityTypeDescriptor` (compile-time discovered modular entity registry + `SystemGuid` constants), `DefinedType` + `DefinedValue` aggregate, recursive `Category` tree aggregate, Redis/memory read caches, Admin UI screens (`DefinedTypeList`, `DefinedTypeDetail`, `CategoryTreeView`).
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `blazing-fast-dotnet`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`.

#### `W1-S4` — Typed `jsonb` Attribute Engine, Attribute Matrices & Core FieldTypes ✅ *(Completed)*
* **Scope:** `AttributeDefinition` (global, entity-type, or entity-qualifier scoped), `AttributeMatrixTemplate` (repeatable structured sub-rows in `jsonb`), `IHasAttributes` `jsonb` column convention + GIN index helper, and modular `IFieldTypePlugin` registry (25 foundational FieldTypes: Text, Memo, Boolean, Integer, Decimal, Currency, Date, DateTime, DateRange, SingleSelect, MultiSelect, DefinedValue, Category, KeyValueList, Json/Code, Color, Url, Email, Phone, Rating, Matrix) + dynamic `<AttributeValuesEditor />` and `<AttributeValuesSummary />` Blazor components.
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `modern-csharp`, `blazing-fast-dotnet`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W1-S5` — Branches (Sites), PostGIS Locations & Physical Devices ✅ *(Completed — Generalized in `W3-S0`)*
* **Scope:** `Branch` (`BranchSchedule`, `BranchTopic` — representing operating Sites, Branches, Chapters, Centers, or Offices), `Location` hierarchy (buildings, rooms, named locations, street addresses) with `NetTopologySuite` **PostGIS** `geography(Point, 4326)` & `geography(Polygon, 4326)` geofences, `IAddressVerificationProvider` / geocoding hook, `Device` (kiosks, label printers, check-in tablets, IP/Geo matching).
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `dotnet-webapi`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`.

#### `W1-S6` — Schedules & iCalendar Recurrence Engine ✅ *(Completed)*
* **Scope:** `Schedule` aggregate (`iCalendarContent` RFC 5545 RRULEs, check-in start/end offsets, `ScheduleDate` materialized horizon table for fast SQL range joins, `ScheduleCategoryExclusion`), friendly schedule text formatter, `<ScheduleBuilderDialog />` Blazor component.
* **Authoring Skills:** `architecture`, `modern-csharp`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `blazing-fast-dotnet`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W1-S7` — BinaryFiles, Documents, Electronic Signatures & RustFS Storage ✅ *(Completed)*
* **Scope:** `IStorageProvider` + `RustFsStorageProvider` (S3 presigned URLs & streaming uploads), `BinaryFileType`, `BinaryFile`, image crop/avatar processor, `DocumentType`/`Document`, `SignatureDocumentTemplate`/`SignatureDocument` electronic signature capture, `<FileUploader />` & `<ImageEditor />` Blazor components.
* **Authoring Skills:** `architecture`, `aspnet-core`, `entity-framework-core`, `dotnet-webapi`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`.

#### `W1-S8` — Audit History, Notes, Tags, EntitySets & User Preferences ✅ *(Completed)*
* **Scope:** Structured `HistoryLog` (`HistoryLogEntry` domain change timeline), `NoteType`/`Note`/`NoteAttachment`/`NoteWatch` threaded notes timeline component, personal & organizational `Tag`/`TaggedItem` autocomplete pill bar, ephemeral/saved `EntitySet` bulk selection sets, `PersonPreference` key-value store.
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `modern-csharp`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`.

---

### Wave 2 — Constituent CRM & Groups/Teams
*Goal: Deliver complete Person & Organization 360 profiles, Household/Unit & Apache AGE relationship graphs, deduplication/merge, hierarchical groups/chapters/teams, volunteer/shift scheduling, and attendance.*

#### `W2-S1` — Person Aggregate, Contact Info, Metaphones & Fast Person Search ✅ *(Completed)*
* **Scope:** `Crm` module (`crm` schema): `Person` aggregate (record type Person/Business/SystemUser, connection status, record status, marital status, BirthDate, graduation/grade calculation, deceased handling, photo, `PrimaryBranchId`), `PhoneNumber` (E.164 normalization, SMS opt-in), `PersonPreviousName`, `PersonSearchKey` (alternate emails/barcodes/external IDs), `Metaphone` / `MetaFirstNameGenderLookup` / `MetaNickNameLookup`, PostgreSQL `pg_trgm` + phonetic instant search bar & Person Profile header.
* **Authoring Skills:** `architecture`, `modern-csharp`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `blazing-fast-dotnet`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W2-S2` — Households (Member/Billing Units) & Apache AGE Relationship Graph ✅ *(Completed — Generalized in `W3-S0`)*
* **Scope:** First-class `Household` aggregate (Adult/Child roles, `IsPrimaryContact`, `BranchId`, Mapped Mailing/Home `Location`s, Combined Contribution/Billing Unit flag, multi-household membership) + `KnownRelationship` & `PeerNetwork` graph (clean-room bounded saturation growth & exponential half-life decay) synchronized to **PostgreSQL Apache AGE** (`crm_graph`) for multi-hop relationship traversal, plus Household UI cards on Person Profile.
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `modern-csharp`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W2-S3` — Person Duplicate Detection, Redirect Ledger & Cross-Module Merge ✅ *(Completed — Generalized in `W3-S0`)*
* **Scope:** Scheduled `PersonDuplicateScannerJob` (clean-room completeness-adjusted weighted attribute similarity scoring across email/phones/address/name/DOB/suffix), `PersonRedirect` ledger (maps retired `PersonId`s and optional `ExternalRecordGuid`s to surviving `PersonId`), side-by-side Blazor `<PersonMergePage />`, and `PersonMergedIntegrationEvent` via Outbox so every downstream module (`Core`, `Groups`, `Finance`, `CheckIn`, etc.) re-points `PersonId` cleanly.
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `blazing-fast-dotnet`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W2-S4` — Signals, Extensible Badges, Assessments & Background Screening ✅ *(Completed — Generalized in `W3-S0`)*
* **Scope:** `SignalType` / `PersonSignal` (top-of-profile safety/compliance/VIP banners), modular `IPersonBadge` Blazor component bar, `AssessmentType` / `Assessment` (Team Strengths, DISC, Conflict Style, Motivators, Emotional Intelligence), `BackgroundCheck` + `IBackgroundCheckProvider` (including modular Checkr webhook adapter), `IdentityVerification`, and `PersonViewed` audit log.
* **Authoring Skills:** `architecture`, `aspnet-core`, `dotnet-webapi`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`.

#### `W2-S5` — GroupTypes, Group Hierarchy, Locations & PostGIS Group Finder ✅ *(Completed — Generalized in `W3-S0`)*
* **Scope:** `Groups` module (`groups` schema): `GroupType` (`GroupTypeRole`, allowed child GroupTypes, `GroupsRequireBranch`, location modes, attendance rules), `Group` tree aggregate (chapters, committees, cohorts, classes, volunteer teams), `GroupLocation`, `GroupDemographicType`/`Value`, `GroupHistorical` snapshots, Admin `GroupTreeView`/`GroupDetail` shell, and public/internal PostGIS radius/polygon `GroupFinder` + map view.
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `modern-csharp`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`.

#### `W2-S6` — Group Memberships, Requirements, Placement & Sync ✅ *(Completed)*
* **Scope:** `GroupMember` lifecycle (Active, Inactive, Pending, capacity/role limits, `GroupMemberHistorical`), `GroupRequirementType` / `GroupRequirement` / `GroupMemberRequirement` (background screening/certification/attribute prerequisites), `GroupPlacement` board, `GroupSimpleRegister`, and scheduled `GroupSync` reconciliation.
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `modern-csharp`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W2-S7` — Volunteer & Shift Roster Scheduler & RSVPs ✅ *(Completed — Generalized in `W3-S0`)*
* **Scope:** `GroupMemberScheduleTemplate`, `GroupLocationScheduleConfig` (min/desired/max staffing thresholds), `GroupMemberAssignment`, `PersonScheduleExclusion` (blackout dates), `GroupScheduleExclusion`, interactive Coordinator Roster Grid + Volunteer/Staff Schedule Toolbox & Decline/Accept RSVP links (`Rsvp`).
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `modern-csharp`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W2-S8` — Attendance Occurrences & Roster Checklists ✅ *(Completed — Generalized in `W3-S0`)*
* **Scope:** `AttendanceOccurrence` (Group + Location + Schedule + OccurrenceDate, configurable `WeekEndDate` via `ReportingWeekCalculator`, `DidNotOccur`), `Attendance` records (`BranchId`), mobile/tablet-friendly Roster Attendance checklist UI, Branch/Program metric summary hooks, and `PersonAttendedIntegrationEvent`.
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `dotnet-webapi`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`.

---

### Wave 3 — Clean-Room Generalization, Check-In & Communications
*Goal: Complete the organization-agnostic clean-room refactor, then deliver mission-critical Kiosk/Facility Check-In, ZPL badge printing, the Razor & safe merge-token template engine, and multi-channel communications.*

#### `W3-S0` — Clean-Room & Organization-Agnostic Generalization ✅ *(Completed)*
* **Scope:** Disconnect external ChMS reference code (`refs/`); generalize `Campus` -> `Branch` (`Site`/`Branch`) across `Identity`, `Core`, `Crm`, `Groups`, and `Host` with EF Core schema migrations (dropping church-only `TitheMetric` and `AverageWeekendAttendance`); replace `SundayDateCalculator` with configurable `ReportingWeekCalculator` (`WeekEndDate`); replace legacy-derived CRM heuristics (`PersonDuplicateScorer`, `HouseholdDomainService`, `PeerNetworkScoreCalculator`, `PersonRedirect`, `CronExpression`) with clean-room, gender-neutral, first-principles implementations; neutralize seeded assessments, screening packages, and UI copy.
* **Authoring Skills:** `mcaf`, `architecture`, `modern-csharp`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `quality-ci`, `csharpier`, `format`, `code-analysis`, `complexity`, `crap-score`.

#### `W3-S1` — Razor Template Renderer, Safe Merge Tokens & Reusable Snippets ✅ *(Completed)*
* **Scope:** `IRazorTemplateRenderer` built on Blazor's native `.NET 10` `HtmlRenderer` (rendering strongly-typed `.razor` component templates to HTML/text with DI support) paired with `IMergeTemplateEngine` for database-stored, staff-editable content (emails, SMS, signature waivers, ZPL/text): compiled token parser (`{{ Field }}` with safe built-in formatters for dates, currency, casing, fallback defaults, and HTML encoding), strict size/recursion limits, reusable `TemplateSnippet` definitions, and read-only DTO merge contexts.
* **Authoring Skills:** `architecture`, `modern-csharp`, `blazing-fast-dotnet`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W3-S2` — Check-In Configuration, Areas, Eligibility Rules & Room Balancing ✅ *(Completed)*
* **Scope:** `CheckIn` module (`checkin` schema): Check-in configuration profiles (youth/childcare safety, member facility access, classes, volunteer shifts, conferences), Area/Group/Location/Schedule matrices, eligibility specifications (Age range, Birthdate, Grade, Membership/Skill tier, Accessibility/Accommodations, Pre-Enrolled), room capacity thresholds, and automated room-balancing domain service.
* **Authoring Skills:** `architecture`, `modern-csharp`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `blazing-fast-dotnet`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W3-S3` — High-Throughput Check-In Session Engine, Kiosk UI & Room/Facility Manager ✅ *(Completed)*
* **Scope:** `AttendanceCheckInSession`, collision-free daily `SecurityPickupCode` generator (configurable alpha/numeric pickup/badge codes with profanity filter), sub-50ms phone-number/household/member-card/barcode search, touch-optimized Blazor Check-In Kiosk UI + Check-Out flow + live Room/Facility Manager roster/override tablet UI + Mobile Check-In Minimal APIs.
* **Authoring Skills:** `architecture`, `blazing-fast-dotnet`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `dotnet-webapi`, `stylelint`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`, `webhint` (Kiosk touch/a11y/perf audit).

#### `W3-S4` — Badge & Label Designer & Zebra ZPL Print Pipeline ✅ *(Completed)*
* **Scope:** `CheckInLabel` aggregate (JSON layout schema + Zebra ZPL compilation), merge fields (Pickup Security Code, Medical/Allergy/Compliance Alerts, Room/Zone, Guardian Receipt, Badge/Tier icons), server TCP raw-socket printer dispatch (for local/VPN Zebra printers) + browser local-print/WebUSB JS interop module, and visual Blazor Label Preview/Editor.
* **Authoring Skills:** `architecture`, `modern-csharp`, `blazing-fast-dotnet`, `blazor`, `fluentui-blazor-usage`, `biome`.
* **Verification Skills:** `csharpier`, `code-analysis`, `biome`, `complexity`, `crap-score`.

#### `W3-S5` — Communication Transports, System Notifications & Snippets ✅ *(Completed)*
* **Scope:** `Communication` module (`comms` schema): `ICommunicationTransport` modular contracts + providers (SMTP, SendGrid, Mailgun, Twilio SMS, Push Notification stub, Dev In-Memory Sink), `SystemCommunication` transactional email/SMS templates (wired to Identity/Group/Finance/Workflow events), `SystemPhoneNumber`, `PersonalDevice` push tokens.
* **Authoring Skills:** `architecture`, `aspnet-core`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `htmlhint`.
* **Verification Skills:** `csharpier`, `code-analysis`, `htmlhint`, `complexity`.

#### `W3-S6` — Bulk Communications, Email Builder, Recipients & Two-Way SMS Pipeline ✅ *(Completed)*
* **Scope:** `CommunicationTemplate`, `EmailSection` structured email composer, `Communication` lifecycle (Draft -> Pending Approval -> Approved -> Sending -> Sent), `CommunicationRecipient` batch sending job, bounce/open/click webhook Minimal APIs, unsubscribe/preference center management, two-way SMS Conversations inbox (`CommunicationResponse`) + `SmsPipeline`/`SmsAction` keyword responder.
* **Authoring Skills:** `architecture`, `aspnet-core`, `dotnet-webapi`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `htmlhint`, `stylelint`.
* **Verification Skills:** `csharpier`, `code-analysis`, `htmlhint`, `complexity`, `crap-score`.

#### `W3-S7` — Communication Flows & Automated Drip Campaigns ✅ *(Completed)*
* **Scope:** `CommunicationFlow` (multi-step timed onboarding/renewal/engagement drip sequences, exit/conversion conditions), `CommunicationFlowInstance`, `CommunicationFlowInstanceRecipient`, `CommunicationFlowConversion`, Flow Builder & Performance Analytics UI.
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `modern-csharp`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W3-S8` — Smart Wallet Passes (Apple `.pkpass` & Google Wallet Member/Event Cards) ✅ *(Completed)*
* **Scope:** `CheckIn` module (`Slices/WalletPasses/`) + `IWalletPassProvider` strategy (`ApplePkpassProvider` using native `.NET 10` PKCS#7 detached signature packaging + Apple PassKit WebService device registration/push-update endpoints, `GoogleWalletJwtProvider`, and deterministic `PreviewWalletPassProvider` when pass certificates are unconfigured): `WalletPassTemplate`, `IssuedWalletPass` (`SerialNumber`, `AuthTokenHash`, `ScanToken`, dynamic fields for Membership Tier, Punch/Credit Balance, Next Shift/Booking, or Event Ticket), `<50ms` Kiosk scan resolution hook, email/SMS pass claim links, and Staff/Member Pass Preview & Issuance UI.
* **Authoring Skills:** `architecture`, `modern-csharp`, `aspnet-core`, `dotnet-webapi`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `blazing-fast-dotnet`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

---

### Wave 4 — Finance (Funds, Dues, Fees, Invoices & Grants) & Events
*Goal: Deliver a dual-capable financial ledger supporting both tax-deductible charitable funds/pledges/grants and standard membership dues, program fees, and invoices—paired with calendars and event registrations.*

#### `W4-S1` — Financial Accounts, Funds, Revenue Categories, Organizations & Pledges ✅ *(Completed)*
* **Scope:** `Finance` module (`finance` schema): hierarchical `FinancialAccount` (`AccountKind`: CharitableFund, MembershipDues, ProgramFee, GrantFund, OperatingRevenue; `IsTaxDeductible` flag, `BranchId` mapping, GL/cost-center codes, start/end dates), `Money` value object (strict decimal ISO-4217 currency invariants), Organization/Sponsor CRM facade (`OrganizationList`, `OrganizationDetail`), and `FinancialPledge` / commitment tracking + analytics.
* **Authoring Skills:** `architecture`, `modern-csharp`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`.

#### `W4-S2` — Payment Gateways & Tokenized Saved Payment Methods ✅ *(Completed)*
* **Scope:** `FinancialGateway` configuration, modular `IPaymentGateway` (hosted tokenizer JS interop, ACH/card charge, refund, customer vault, recurring subscription sync), Test Gateway + modular `NmiGatewayProvider` / Stripe-ready contract, `SavedPaymentMethod` & hashed routing/account `BankAccountIdentifier` (for check/ACH matching).
* **Authoring Skills:** `architecture`, `aspnet-core`, `dotnet-webapi`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `biome`.
* **Verification Skills:** `csharpier`, `code-analysis`, `biome`, `complexity`, `crap-score`.

#### `W4-S3` — Financial Batches, Invoices, Dues/Fee Billings, Transactions & Refunds ✅ *(Completed)*
* **Scope:** `Invoice` aggregate (line items, due dates, partial/full balance tracking for membership dues, tuition, facility rentals, or sponsorships), `FinancialBatch` aggregate (Pending -> Open -> Closed state machine, control amount/count reconciliation, audit locks), `FinancialTransaction` + `FinancialTransactionDetail` multi-account splits (settling charitable contributions, direct fees, or open `Invoice`s), `FinancialTransactionImage` (RustFS encrypted check/remittance images), `FinancialTransactionRefund`, Batch/Invoice/Transaction staff screens + check-scanner Minimal API.
* **Authoring Skills:** `architecture`, `modern-csharp`, `entity-framework-core`, `dotnet-webapi`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W4-S4` — Recurring Contributions, Membership Subscriptions & Cadence Alerts ✅ *(Completed)*
* **Scope:** `RecurringScheduleTransaction` (recurring charitable gifts AND recurring membership dues/retainers), gateway webhook & nightly settlement sync job, member/donor self-service billing & recurring management component, and `TransactionCadenceAlertType` / `TransactionCadenceAlert` (churn/lapse risk and unusual spike detector).
* **Authoring Skills:** `architecture`, `aspnet-core`, `dotnet-webapi`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `blazing-fast-dotnet`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W4-S5` — Tax Receipts, Account Statements & Assistance / Grant Case Management ✅ *(Completed)*
* **Scope:** `FinancialStatementTemplate` (compiled `.razor` layout via `HtmlRenderer` + merge tokens -> print-CSS / PDF generation separating **Tax-Deductible Charitable Receipts** from **Non-Deductible Dues/Fee/Invoice Statements** for individuals, households, and corporate sponsors, online download + batch run), plus `AssistanceProgram`, `AssistanceCase` (`AssistanceDocument`, `AssistanceDisbursement` — supporting community aid, scholarships, fee waivers, and micro-grants).
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `htmlhint`, `stylelint`.
* **Verification Skills:** `csharpier`, `code-analysis`, `htmlhint`, `complexity`.

#### `W4-S6` — Peer-to-Peer & Project Campaign Fundraising ✅ *(Completed)*
* **Scope:** Campaign / project / team fundraising configuration on Groups + Financial Accounts, participant & team goal overrides, public contribution portal (supporting donor privacy options and participant fundraising credit), `CampaignLeaderToolbox` & progress widgets.
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`.

#### `W4-S7` — Event Calendars, Occurrences & Live Interactive Sessions ✅ *(Completed)*
* **Scope:** `Events` module (`events` schema): `EventCalendar`, `EventItem`, `EventItemAudience`, `EventItemOccurrence` (`BranchId`, `LocationId`, `ScheduleId`), `iCal` feed endpoint, and real-time SignalR `InteractiveSession` (`InteractiveAction`, `Occurrence`, `Response` for live town-hall/conference/classroom polls, Q&A, and stage projections).
* **Authoring Skills:** `architecture`, `aspnet-core`, `dotnet-webapi`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`.

#### `W4-S8` — Event Registrations, Dynamic Forms, Fees, Invoices, Discounts & Waitlists ✅ *(Completed)*
* **Scope:** `RegistrationTemplate` (`Form`, `FormField`, `Fee`, `FeeItem`, `Discount`, `GroupPlacement`), `RegistrationInstance`, `RegistrationSession` (seat holds with TTL), `Registration`, `RegistrationRegistrant`, waitlist auto-promotion, deposit/installment/invoice payment integration (`Finance.Contracts`), and multi-step public Registration Wizard.
* **Authoring Skills:** `architecture`, `modern-csharp`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`, `webhint`.

#### `W4-S9` — Commerce Catalog, Member Pricing, Punch Passes & Quid-Pro-Quo Checkout ✅ *(Completed)*
* **Scope:** `Commerce` module (`commerce` schema): `Product` & `ProductVariant` (Digital `RustFS` downloads, Pickup/Flat-Rate physical items, and `PunchPassType` multi-visit/credit packs), `MemberPricingRule` (tier/group automatic discounts), IRS *quid-pro-quo* split mapping (fair-market value routed to non-deductible `OperatingRevenue` account + excess routed to tax-deductible `CharitableFund` account via `Finance.Contracts`), `CommerceOrder` lifecycle, `IssuedPunchPass` balance ledger (syncing to `W3-S8` Wallet Passes), Staff Catalog/Order Manager, and Storefront Checkout UI. *(Scope guardrail: flat/gateway tax hooks and pickup/flat-rate fulfillment—no multi-warehouse WMS or carrier-rate ERP bloat.)*
* **Authoring Skills:** `architecture`, `modern-csharp`, `entity-framework-core`, `dotnet-webapi`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W4-S10` — Bookable Services, 1-on-1 Appointments & Facility/Resource Rentals ✅ *(Completed)*
* **Scope:** `Commerce` module (`Slices/Bookings/`): `BookableService` (1-on-1 coaching, counseling, tutoring, intake, or paid virtual workshops) and `RentableResource` (referencing `Core` `Location` rooms, courts, studios, or equipment), `AvailabilityRule` (provider/location `Schedule` windows, setup/teardown buffers, lead/cancel rules, blackout checks), conflict-free slot engine with TTL reservation holds, member-free vs. paid checkout (`Finance.Contracts` `Invoice`/charge), `ServiceBookingConfirmedIntegrationEvent` (updating Wallet Passes and provisioning virtual rooms), Coordinator Calendar UI, and Public Booking Wizard.
* **Authoring Skills:** `architecture`, `modern-csharp`, `entity-framework-core`, `dotnet-webapi`, `blazor`, `fluentui-blazor-usage`, `blazing-fast-dotnet`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`, `webhint`.

---

### Wave 5 — Engagement Pipelines, Journeys, Care & Workflow
*Goal: Deliver member/constituent onboarding pipelines, milestone journeys, attendance/activity streaks, community care/support intake, and a full dynamic Workflow + Form Builder engine.*

#### `W5-S1` — Engagement Pipelines, Opportunities & Connector Board
* **Scope:** `Engagement` module (`engage` schema): `PipelineType` (`ConnectionType`), `PipelineStage`, `StageAutomation`, `ActivityType`, `EngagementOpportunity` (branch/group/coordinator configs for volunteer intake, membership onboarding, mentorship, or referrals), `EngagementRequest` lifecycle, Kanban/Grid Coordinator Board UI, and public Opportunity Finder/Signup.
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `modern-csharp`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W5-S2` — Journeys (Milestones/Steps), High-Density Streaks & Achievements Engine
* **Scope:** `JourneyProgram` (`StepProgram`), `MilestoneType` (prerequisites), `MilestoneStatus`, `PersonMilestone`, `JourneyCompletion`; `StreakType`, `StreakExclusion`, `Streak` (`Span<byte>` bitmask participation cadence engine); `AchievementType` / `AchievementAttempt` event-driven badge & certification milestone engine.
* **Authoring Skills:** `architecture`, `modern-csharp`, `blazing-fast-dotnet`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W5-S3` — Community Care / Support Requests, Reminders & Following Subscriptions
* **Scope:** `SupportRequest` (configurable categories—e.g., Member Assistance, Wellbeing Check-In, Mentorship, Community Care, or Pastoral/Prayer where desired—with intake, privacy/moderation queue, expiration, threaded comments, guided care-team review queue, and community support counter), `ReminderType` / `Reminder` entity follow-ups + notification bell (`NotificationMessage`), and `Following` digest notifications.
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`.

#### `W5-S4` — Account / Constituent Touchpoints & Relationship Cadence
* **Scope:** Personal portfolio / steward / caseworker `ConstituentContact`, `Touchpoint` cadence scheduler, relationship health updates, and mobile/web My Portfolio outreach queue.
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `dotnet-webapi`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`.

#### `W5-S5` — Workflow State/Activity/Action Engine & Entity Triggers
* **Scope:** `Workflow` module (`workflow` schema): `WorkflowType`, `WorkflowActivityType`, `WorkflowActionType`, `Workflow` state-machine aggregate (`WorkflowActivity`, `WorkflowAction`, `WorkflowLog`, persisted `jsonb` workflow attributes), `WorkflowTrigger` / `AutomationTrigger` (reacting to integration events), background processing worker, and core modular `IWorkflowAction` implementations (Control Flow, Attribute manipulation, CRM/Group/Finance/Pipeline/Communication actions, Razor/Merge Template, Webhook).
* **Authoring Skills:** `architecture`, `modern-csharp`, `blazing-fast-dotnet`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W5-S6` — Workflow Interactive Forms & Visual Form Builder
* **Scope:** `WorkflowActionForm`, `WorkflowActionFormSection`, `WorkflowActionFormField`, `FormBuilderTemplate`, interactive Fluent UI v5 Form Builder, conditional field visibility rules, and `WorkflowEntry` / `MyTasks` inbox UI.
* **Authoring Skills:** `architecture`, `blazor`, `fluentui-blazor-usage`, `entity-framework-core`, `stylelint`, `biome` (plus `eslint`/`sonarjs` only if custom canvas TS is added).
* **Verification Skills:** `csharpier`, `code-analysis`, `biome`, `stylelint`, `complexity`, `webhint`.

#### `W5-S7` — Embedded `LiveKit` Virtual Rooms, Hybrid Sessions & Auto-Attendance
* **Scope:** `VirtualRooms` module (`virtual` schema) + Apache-2.0 `livekit/livekit-server` (+ Egress) in `docker-compose.yml` backed by `Redis 8` and `RustFS` S3: `IVirtualRoomProvider` (`LiveKitRoomProvider` + `InMemoryVirtualRoomProvider` for deterministic tests), `VirtualRoom` & `VirtualRoomSession` linked to `Group` meetings, `Commerce` 1-on-1 bookings/workshops (`ServiceBookingConfirmedIntegrationEvent`), `SupportRequest` tele-care consults, `Events` hybrid sessions, or `LMS` cohorts; short-lived HMAC-SHA256 room JWT minting mapped from `Ruth` ACL/moderator roles; `Biome`-linted LiveKit WebComponent Blazor island; and verified LiveKit webhook endpoints (`participant_joined`, `participant_left`, `egress_ended`) that auto-record `Attendance` after configurable dwell thresholds and archive `RustFS` recordings for `Wave 6` `pgvector` transcript search.
* **Authoring Skills:** `architecture`, `modern-csharp`, `aspnet-core`, `dotnet-webapi`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `biome`, `stylelint`.
* **Verification Skills:** `csharpier`, `code-analysis`, `biome`, `stylelint`, `complexity`, `crap-score`.

---

### Wave 6 — CMS, Reporting, LMS, Channels & AI
*Goal: Deliver external website/member-portal CMS, declarative DataViews/Reporting, Operational & Financial Analytics, LMS, Mobile/Display backends, and `pgvector` AI agents.*

#### `W6-S1` — Dynamic CMS Engine (`Site -> Layout -> Page -> Zone -> Block`)
* **Scope:** `Cms` module (`cms` schema): `Site`, `SiteDomain` multi-host routing, `Theme` (CSS custom-property bundles), `Layout`, `Page`, `PageRoute`, `PageContext`, `BlockType`, `Block`, `HtmlContent` (versioned + approval), `<CmsPageHost />` (`Static SSR` + interactive `<DynamicComponent>` islands implementing `IRuthBlock`), admin inline Zone/Block configurator, `PageShortLink` URL shortener + QR generator, and `RequestFilter`.
* **Authoring Skills:** `architecture`, `aspnet-core`, `blazor`, `fluentui-blazor-usage`, `entity-framework-core`, `stylelint`, `htmlhint`, `webhint`.
* **Verification Skills:** `csharpier`, `code-analysis`, `stylelint`, `htmlhint`, `webhint`, `complexity`, `crap-score`.

#### `W6-S2` — Content Channels, Media Accounts, Bookmarks & Persisted Datasets
* **Scope:** `ContentChannelType`, `ContentChannel`, `ContentChannelItem` (`Slug`, `Association`, scheduled publishing, RSS/JSON feeds), `ContentTopic`, `MediaAccount` / `MediaFolder` / `MediaElement` (Vimeo/YouTube/HLS sync provider), `PersonalLinkSection` / `PersonalLink` staff bookmarks bar, and `PersistedDataset` cached JSON/query datasets consumed by Razor CMS blocks and typed Minimal API endpoints.
* **Authoring Skills:** `architecture`, `aspnet-core`, `dotnet-webapi`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `blazing-fast-dotnet`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`.

#### `W6-S3` — Interactions, Personalization Segments & Adaptive Content
* **Scope:** High-throughput buffered `Interaction` ingestion (`InteractionChannel`, `InteractionComponent`, `InteractionSession`), `PersonalizationSegment` (affinity, request filters, DataView membership cached in Redis bitsets/sets), and `AdaptiveMessage` targeted content delivery.
* **Authoring Skills:** `architecture`, `blazing-fast-dotnet`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W6-S4` — Mobile & Digital Signage / TV Application Backends
* **Scope:** Mobile app configuration (`MobileApplication`, `MobileLayout`, `MobilePage`, `MobileDeepLink`) + versioned Minimal API contract for native mobile shells, and Digital Signage / Apple TV / Roku feed endpoints.
* **Authoring Skills:** `architecture`, `dotnet-webapi`, `aspnet-core`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`.

#### `W6-S5` — Declarative DataViews, Reports & Merge Document Templates
* **Scope:** `Reporting` module (`reporting` schema): `DataView` + `DataViewFilter` declarative **Query AST** (AND/OR/NOT filter trees compiling to module EF Core queries, `jsonb` attribute predicates, PostGIS spatial filters, and cross-module `PersonId` set intersections), `DataViewPersistedValue` background materializer, `Report` + `ReportField` column selector (`IFieldSelectPlugin`), CSV/Excel export, and DOCX/HTML `MergeTemplate` generator.
* **Authoring Skills:** `architecture`, `modern-csharp`, `entity-framework-core`, `blazing-fast-dotnet`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

#### `W6-S6` — Operational Metrics, Executive Dashboards & Star-Schema Projections
* **Scope:** `MetricCategory`, `Metric`, `MetricPartition`, `MetricValue` (manual `OperationalMetricsEntry` + scheduled SQL/Query calculation jobs), `DynamicChart` / KPI widgets, `RevenueAndImpactOverview` & `ExecutiveInsights` dashboards (covering donations, dues/MRR, retention, attendance, and program outcomes), and materialized PostgreSQL star-schema views/tables for BI tools.
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazing-fast-dotnet`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`.

#### `W6-S7` — Learning Management System (LMS & Certifications)
* **Scope:** `Lms` module (`lms` schema): `LearningProgram` (On-Demand vs. Term/Cohort), `LearningCourse` + prerequisites, `LearningTerm`, `LearningGradingSystem` / `Scale`, `LearningClass`, `LearningClassContentPage`, `LearningClassAnnouncement`, `LearningActivity` (Video, Article, File Upload, Assessment, Acknowledgment), `LearningParticipant`, `LearningActivityCompletion`, Facilitator Grading Queue, and Public Course Catalog / Learner Portal.
* **Authoring Skills:** `architecture`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`, `modern-csharp`, `webhint`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`, `webhint`.

#### `W6-S8` — Universal Search, Content Collections & `pgvector` AI Agents
* **Scope:** `Ai` module (`ai` schema): `ContentCollection` + `ContentCollectionSource` indexing pipeline combining PostgreSQL `tsvector` full-text search with **`pgvector` HNSW semantic embeddings** (respecting `ISecured` ACL filters at query time), global Spotlight Search modal in Staff Shell, modular `IAiProvider` (`OpenAiProvider`), `AIAgent`, `AISkill`, `AISkillTool` (typed read/write tools invoking Ruth Application Queries/Commands under the caller's ACL permissions), and `AIAgentSession` chat drawer.
* **Authoring Skills:** `architecture`, `modern-csharp`, `entity-framework-core`, `blazing-fast-dotnet`, `blazor`, `fluentui-blazor-usage`, `dotnet-webapi`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`.

---

### Wave 7 (Optional) — Universal Bulk Data Import & Onboarding Pipeline
*Goal: Provide a high-throughput, repeatable bulk import engine so organizations migrating from spreadsheets, CRMs, association platforms, or legacy databases can onboard cleanly onto `Ruth`.*

#### `W7-S1` — `Ruth.DataImport` Streaming CSV/JSON Bulk Import & Mapping Engine
* **Scope:** Admin import wizard + CLI job ingesting CSV/JSON/ZIP packages via streaming parsers + PostgreSQL `COPY` binary bulk import: configurable column-to-field & `ExternalRecordGuid` mapping across Branches, Locations, People, Households, Groups, Memberships, Financial Accounts, Invoices, and Transactions, packing custom columns into validated `jsonb` attributes, converting WKT/lat-lng to PostGIS, and producing a post-import reconciliation checksum report.
* **Authoring Skills:** `architecture`, `modern-csharp`, `blazing-fast-dotnet`, `entity-framework-core`, `blazor`, `fluentui-blazor-usage`.
* **Verification Skills:** `csharpier`, `code-analysis`, `complexity`, `crap-score`, `roslynator`.

---

## 7. End-of-Wave Consolidation Checklist

At the completion of each Wave (`Wave 0` .. `Wave 6`) before advancing:
1. Run `roslynator` unused-symbol / dead-code sweep across newly added modules (`roslynator`).
2. Run `Ruth.ArchitectureTests` verifying zero cross-module `Domain` or `Infrastructure` leaks (`architecture`).
3. Run full Cobertura test suite + `crap-score` audit on every domain service/aggregate with cyclomatic complexity >= 8 (`complexity`, `crap-score`).
4. Run `webhint` accessibility, header, and performance checks against all newly added browser routes (`webhint`).
5. Update module-local `AGENTS.md` files only where durable module-specific invariants emerged (`mcaf`).
