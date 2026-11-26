using Ruth.Modules.Tenancy.Contracts.Hosts;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Host.Middleware;

/// <summary>Scoped per-request and per-circuit white-label branding snapshot resolved from the incoming HTTP Host.</summary>
public sealed class TenantWorkspaceBrandingState
{
    /// <summary>Gets the resolved host classification.</summary>
    public ResolvedHostKind HostKind { get; private set; } = ResolvedHostKind.TenantWorkspace;

    /// <summary>Gets the resolved organization display name when available.</summary>
    public string? OrganizationName { get; private set; }

    /// <summary>Gets the resolved tenant slug when available.</summary>
    public string? TenantSlug { get; private set; }

    /// <summary>Gets the resolved tenant lifecycle stage.</summary>
    public TenantLifecycleStage LifecycleStage { get; private set; } =
        TenantLifecycleStage.Unresolved;

    /// <summary>Gets the neutral white-label title to render in shell headers, footers, and page titles.</summary>
    public string BrandTitle =>
        !string.IsNullOrWhiteSpace(OrganizationName) ? OrganizationName
        : HostKind == ResolvedHostKind.PlatformOperator ? "Platform Operations"
        : HostKind == ResolvedHostKind.PlatformSignup ? "Nonprofit Workspace Setup"
        : "Organization Workspace";

    /// <summary>Populates the branding state from the host resolution outcome.</summary>
    public void Initialize(ResolvedHostResponse resolved)
    {
        ArgumentNullException.ThrowIfNull(resolved);
        HostKind = resolved.Kind;
        OrganizationName = resolved.OrganizationName;
        TenantSlug = resolved.TenantSlug;
        LifecycleStage = resolved.LifecycleStage;
    }

    /// <summary>Updates the cached organization display name from onboarding state when not set on the host record.</summary>
    public void SetOrganizationNameIfEmpty(string? organizationName)
    {
        if (
            string.IsNullOrWhiteSpace(OrganizationName)
            && !string.IsNullOrWhiteSpace(organizationName)
        )
        {
            OrganizationName = organizationName.Trim();
        }
    }
}
