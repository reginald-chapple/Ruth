using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Contracts.Hosts;

/// <summary>Immutable host resolution outcome used by HTTP host-routing middleware and Blazor circuits.</summary>
/// <param name="Kind">The resolved host classification.</param>
/// <param name="TenantId">The resolved tenant identifier when <paramref name="Kind" /> is <see cref="ResolvedHostKind.TenantWorkspace" />.</param>
/// <param name="TenantSlug">The resolved tenant slug when <paramref name="Kind" /> is <see cref="ResolvedHostKind.TenantWorkspace" />.</param>
/// <param name="OrganizationName">The organization display name for white-label chrome.</param>
/// <param name="LifecycleStage">The tenant lifecycle stage.</param>
/// <param name="Hostname">The normalized lowercase hostname without port.</param>
public sealed record ResolvedHostResponse(
    ResolvedHostKind Kind,
    TenantId TenantId,
    string? TenantSlug,
    string? OrganizationName,
    TenantLifecycleStage LifecycleStage,
    string Hostname
);
