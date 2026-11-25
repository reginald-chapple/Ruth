# AGENTS.md — `Communication` Module

## Purpose & Boundaries
- Owns the `comms` PostgreSQL schema across two projects: `Ruth.Modules.Communication.Contracts` and `Ruth.Modules.Communication`.
- `Ruth.Modules.Communication.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, or Redis (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.Communication.Contracts`.

## Durable Invariants
- Wave 3 (`W3-S1`) establishes native `.NET 10` Razor component rendering (`IRazorTemplateRenderer` / `IRazorTemplateRegistry` backed by Blazor's `HtmlRenderer`), the pure-C# safe declarative merge-token engine (`IMergeTemplateEngine`), and persisted reusable `TemplateSnippet` definitions (`comms.template_snippets`).
- Never introduce Lava/Liquid engines or arbitrary runtime C# string compilation (`RazorLight` / Roslyn eval) for database-stored templates; all dynamic template rendering uses compiled `.razor` components plus safe declarative merge tokens over read-only DTO contexts.
- `IMergeTemplateEngine` enforces strict template length, output length, token count, conditional nesting depth, and snippet recursion/cycle limits, and HTML-encodes all interpolated context values in `Html` mode.
- System `TemplateSnippet` definitions (`IsSystem == true`) cannot be deleted.

## Verification
```bash
dotnet build src/Modules/Communication/Ruth.Modules.Communication/Ruth.Modules.Communication.csproj -warnaserror
dotnet test tests/Ruth.Modules.Communication.Tests/Ruth.Modules.Communication.Tests.csproj
```
