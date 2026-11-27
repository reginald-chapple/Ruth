# AGENTS.md — `Search` Module

## Purpose & Boundaries
- Owns the `search` PostgreSQL schema across two projects: `Ruth.Modules.Search.Contracts` and `Ruth.Modules.Search`.
- `Ruth.Modules.Search.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, Redis, or `Ruth.Persistence` (enforced by `Ruth.ArchitectureTests`).
- `Ruth.Modules.Search.Contracts` may only depend on `Ruth.SharedKernel` (pure C# contracts including `ISearchDocumentProvider`).

## Durable Invariants
- **W6-S8 (`ContentCollection`, `ContentCollectionSource`, `SearchDocument`, PostgreSQL Full-Text + Trigram Search):**
  - Powered deterministically by PostgreSQL stored weighted `tsvector` (`A`=Title, `B`=Subtitle+Tags, `C`=Summary, `D`=BodyText), `websearch_to_tsquery`, `ts_rank_cd`, `ts_headline`, and `pg_trgm` GIN trigram similarity indexes—zero external LLM/AI dependencies.
  - `SearchSnippetSanitizer` HTML-encodes `ts_headline` output before restoring safe `<mark>` spans, preventing XSS from indexed document bodies.
  - Nine modular `ISearchDocumentProvider` implementations index `Crm`, `Groups`, `Finance`, `Events`, `Commerce`, `Engagement`, `Workflow`, `Cms`, and `Lms` strictly through `*.Contracts` queries, paired with Outbox integration-event consumers and SHA-256 content-hash idempotency.
  - Query-time `IEntityAuthorizationService` ACL filtering evaluates every candidate hit before returning Spotlight or Public CMS results.
- **Multi-Tenant & RLS Isolation (`W7-S6`):** Every table in `search` enforces `ITenantScoped` + PostgreSQL 18 `ENABLE` + `FORCE ROW LEVEL SECURITY` (`ruth_app`), and `PostgreSqlFullTextSearchEngine` binds `tenant_id` on every full-text and trigram query.

## Verification
```bash
dotnet build src/Modules/Search/Ruth.Modules.Search/Ruth.Modules.Search.csproj -warnaserror
dotnet test tests/Ruth.Modules.Search.Tests/Ruth.Modules.Search.Tests.csproj
```
