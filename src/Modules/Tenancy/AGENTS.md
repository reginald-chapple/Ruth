# AGENTS.md — Ruth.Modules.Tenancy (`platform` Control Plane)

## Purpose

`Ruth.Modules.Tenancy` owns the multi-tenant SaaS control plane (`platform` PostgreSQL schema), including:

- Tax-exempt organization `Tenant` lifecycle (`Sandbox` -> `PendingVerification` -> `Live` / `Suspended`) and lifecycle domain events.
- Hostname routing and DNS TXT custom domain ownership verification (`TenantDomain`, `ITenantHostResolver`, and `IDnsTxtVerificationProvider`).
- IRS Exempt Organizations Business Master File (EO BMF / Pub 78) charity verification (`IIrsCharityVerificationProvider` + `IrsExemptVerification`).
- Persisted Stripe Connect Express onboarding & payout readiness (`IStripeConnectProvider`, `MockStripeConnectProvider`, and `StripeConnectAccount`).
- Platform Operator review queue (`PlatformOperator`) strictly isolated to the operator host (`ops.<platform-domain>`).

## Local Invariants

1. `Ruth.Modules.Tenancy.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, or Redis (enforced by `Ruth.ArchitectureTests`).
2. `TenancyDbContext` sets `IsTenantScopedSchema => false` and owns the `platform` schema exclusively under `ruth_migrator`; runtime `ruth_app` tenant connections have zero privileges on `platform`.
3. Control-plane commands/queries implement `IPlatformControlPlaneRequest`; tenant self-service compliance commands (`GetCurrentTenantComplianceQuery`, `SubmitTenantForGoLiveCommand`, etc.) execute inside the authenticated tenant administrator's own `ITenantContext`.
4. Platform operator endpoints must return `404 Not Found` whenever `ITenantContext.IsPlatformOperatorHost` is `false` so tenant custom domains never leak platform control-plane routes, and review actions must bind operator identity from the authenticated operator principal.
5. No public organization directory endpoints may ever be added to this module.

## Verification

```bash
dotnet build src/Modules/Tenancy/Ruth.Modules.Tenancy/Ruth.Modules.Tenancy.csproj -warnaserror
dotnet test tests/Ruth.Modules.Tenancy.Tests/Ruth.Modules.Tenancy.Tests.csproj
```
