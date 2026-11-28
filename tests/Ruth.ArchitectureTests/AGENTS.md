# AGENTS.md — `Ruth.ArchitectureTests`

## Purpose & Boundaries
- Automated `NetArchTest.Rules` + `xUnit` suite enforcing Modular Monolith dependency rules:
  1. `Ruth.SharedKernel` has zero web, persistence, or module dependencies.
  2. Module `.Domain` namespaces never depend on `.Infrastructure`, `.Slices`, EF Core, ASP.NET Core, or Fluent UI.
  3. Module assemblies never reference another module's internal assembly (only `*.Contracts` and `Ruth.SharedKernel`).
  4. Non-abstract `*Handler`, `*Request`, and `*Response` classes are `sealed`.
  5. Every entity mapped in a tenant-scoped `ModuleDbContext` implements `ITenantScoped`, while control-plane entities remain isolated in `Ruth.Modules.Tenancy` (`platform` schema).
  6. Every mapped HTTP and SignalR endpoint across all 16 modules explicitly declares `IAuthorizeData` or is an audited intentional `[AllowAnonymous]` route (`EndpointAuthorizationArchitectureTests`).
- Whenever a new `Ruth.Modules.<Context>` project is created, add a `<ProjectReference>` here so its assembly is loaded and verified automatically.

## Applicable Skills
- Primary Skills: `architecture`, `quality-ci`, `code-analysis`.

## Verification
```bash
dotnet test tests/Ruth.ArchitectureTests/Ruth.ArchitectureTests.csproj -warnaserror
```
