# AGENTS.md — `Ruth.Host`

## Purpose & Boundaries
- ASP.NET Core 10 composition root, white-label multi-tenant host resolution middleware (`TenantResolutionMiddleware`), stealth header/cookie guards (`__Host-` host-only cookies, zero platform fingerprinting), health/OpenAPI endpoints, self-serve IRS tax-exempt nonprofit signup & operator approval console, and Fluent UI Blazor v5 staff/CMS host shell.
- Registers Bounded Context modules and modular provider assemblies via DI; contains **no** domain business logic of its own.

## Entry Points & Applicable Skills
- Entry points: `Program.cs`, `HostAssembly.cs`
- Primary Skills: `aspnet-core`, `blazor`, `fluentui-blazor-usage`, `dotnet-webapi`, `biome`, `stylelint`, `webhint`.
- Local Risks: Never block async pipelines; preserve exact middleware ordering; fail fast in `Production` if default dev secrets remain configured.
- **Stealth Cookie & Circuit Invariants (`W7-S7`):** HTTPS requests enforce RFC 6265bis `__Host-sid` (`StealthHostCookieManager`) and `__Host-csrf` (`StealthResponseHeadersMiddleware`) while rejecting unprefixed `sid`/`csrf` subdomain cookie-tossing; `TenantCircuitHandler` + `TenantCircuitState` and `TenantSignalRHubFilter` immutably bind handshake `TenantExecutionState` and propagate circuit `ClaimsPrincipal` into `HttpContextPrincipalTenantValidator` across inbound activities; `Routes.razor` blocks cross-plane component rendering between `TenantWorkspace`, `PlatformOperator`, and `PlatformSignup`.
- **Headers, Proxy Trust, Realtime CSWSH & Rate Limiting (`W8-S2`, `W8-S3`):** `StealthResponseHeadersMiddleware` emits a per-request 128-bit hex CSP nonce (`App.razor` `<ImportMap nonce>` & `<script nonce>`), strict `Content-Security-Policy` (preserving stricter endpoint-level `sandbox` CSPs via `TryAdd`), `Permissions-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, and `X-Permitted-Cross-Domain-Policies` while stripping stack-identifying headers; `ProxyAndHostGuardMiddleware` + `HostForwardedHeadersExtensions` enforce RFC 9110 `Host` syntax, explicit `ProxyTrust` CIDR/IP boundaries (rejecting untrusted `X-Forwarded-*` / `X-Host` / `X-Original-URL`), and same-origin WebSocket/`/hubs`/`/_blazor` checks against CSWSH; `/openapi` is unconditionally disabled in `Production`; `HostRateLimitingExtensions` chains per-IP + per-tenant global sliding windows alongside `ruth-auth-strict` and `ruth-kiosk-strict` policies (`429 ProblemDetails` + `Retry-After`).
- **Chiseled Container Liveness Probe (`W8-S5`):** `HostContainerHealthProbe` handles `dotnet Ruth.Host.dll --healthcheck` before host build so shell-less chiseled containers execute native liveness checks under non-root `USER $APP_UID` and `read_only: true` rootfs.

## Verification
```bash
dotnet build src/Ruth.Host/Ruth.Host.csproj -warnaserror
npm run lint:js && npm run lint:css
```
