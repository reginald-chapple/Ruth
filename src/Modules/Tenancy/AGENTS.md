# AGENTS.md — Ruth.Modules.Tenancy (`platform` Control Plane)

## Purpose

`Ruth.Modules.Tenancy` owns the multi-tenant SaaS control plane (`platform` PostgreSQL schema), including:

- Tax-exempt organization `Tenant` lifecycle (`Sandbox` -> `PendingVerification` -> `Live` / `Suspended`).
- Hostname routing and custom domain verification (`TenantDomain` + `ITenantHostResolver`).
- IRS Exempt Organizations Business Master File (EO BMF / Pub 78) charity verification (`IIrsCharityVerificationProvider` + `IrsExemptVerificationRecord`).
- Mockable Stripe Connect Express onboarding & payout readiness (`IStripeConnectProvider` + `MockStripeConnectProvider`).
- Platform Operator review queue (`PlatformOperator`) strictly isolated to the operator host (`ops.<platform-domain>`).

## Local Invariants

1. `TenancyDbContext` sets `IsTenantScopedSchema => false` and owns the `platform` schema exclusively; runtime `ruth_app` tenant connections have zero privileges on `platform`.
2. Control-plane commands/queries implement `IPlatformControlPlaneRequest`; tenant self-service compliance commands (`GetCurrentTenantComplianceQuery`, `SubmitTenantForGoLiveCommand`, etc.) execute inside the authenticated tenant's own `ITenantContext`.
3. Platform operator endpoints must return `404 Not Found` whenever `ITenantContext.IsPlatformOperatorHost` is `false` so tenant custom domains never leak platform control-plane routes.
4. No public organization directory endpoints may ever be added to this module.
