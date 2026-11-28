# AGENTS.md — `Ruth.SharedKernel`

## Purpose & Boundaries
- Owns foundational DDD primitives (`Entity`, `AggregateRoot`, `ValueObject`, Strongly-Typed IDs, `TenantId`, `ITenantScoped`, `ITenantContext`, `AmbientTenantContext`, `TenantExecutionState`, `IActiveTenantRegistry`, `ITenantSeedContributor`, `ICanonicalTenantUrlProvider`, `Result<T>`), `IClock`, zero-magic CQRS/event contracts (including `TenantGuard` decorators blocking unresolved and `Suspended` tenants and enforcing `IPrincipalTenantValidator` across tenant and control planes), and pure-BCL security primitives (`OutboundAddressGuard`, `SafeOutboundHttpHandler`, `WebhookHmacReplayVerifier`, `WebhookReplayDeduplicator`, `SafeRedirectValidator`, `RateLimitPolicyNames`, `ITenantCredentialProtector`, `AesGcmTenantCredentialProtector`, `SensitiveSettingRedactor`, `FileContentSecurityInspector`, `AllowlistHtmlSanitizer`, `EmailHeaderSanitizer`) shared across all modules.
- **Strict Dependency Rule:** Must remain pure .NET (`net10.0`) with **zero** project references and **zero** NuGet dependencies on ASP.NET Core, EF Core, Blazor, Npgsql, or Redis (enforced by `Ruth.ArchitectureTests`).

## Entry Points & Applicable Skills
- Anchor: `SharedKernelAssembly.cs`
- Primary Skills: `architecture`, `modern-csharp`, `blazing-fast-dotnet`, `complexity`, `crap-score`.

## Verification
```bash
dotnet build src/Ruth.SharedKernel/Ruth.SharedKernel.csproj -warnaserror
dotnet test tests/Ruth.SharedKernel.Tests/Ruth.SharedKernel.Tests.csproj
dotnet test tests/Ruth.ArchitectureTests/Ruth.ArchitectureTests.csproj
```
