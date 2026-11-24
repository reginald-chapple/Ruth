# AGENTS.md — `Ruth.Host`

## Purpose & Boundaries
- ASP.NET Core 10 composition root, middleware pipeline, health/OpenAPI endpoints, first-run `/onboarding` guard, and Fluent UI Blazor v5 staff/CMS host shell.
- Registers Bounded Context modules and modular provider assemblies via DI; contains **no** domain business logic of its own.

## Entry Points & Applicable Skills
- Entry points: `Program.cs`, `HostAssembly.cs`
- Primary Skills: `aspnet-core`, `blazor`, `fluentui-blazor-usage`, `dotnet-webapi`, `biome`, `stylelint`, `webhint`.
- Local Risks: Never block async pipelines; preserve exact middleware ordering; fail fast in `Production` if default dev secrets remain configured.

## Verification
```bash
dotnet build src/Ruth.Host/Ruth.Host.csproj -warnaserror
npm run lint:js && npm run lint:css
```
