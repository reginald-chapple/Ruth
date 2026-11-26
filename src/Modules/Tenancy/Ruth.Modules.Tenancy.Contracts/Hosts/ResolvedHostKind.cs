namespace Ruth.Modules.Tenancy.Contracts.Hosts;

/// <summary>Classifies an incoming HTTP Host header resolved by the tenancy control plane.</summary>
public enum ResolvedHostKind
{
    /// <summary>Unknown, unverified, or suspended hostname (must return neutral 404).</summary>
    Unknown = 0,

    /// <summary>Self-service tax-exempt organization signup host.</summary>
    PlatformSignup = 1,

    /// <summary>Dedicated internal Platform Operator console host.</summary>
    PlatformOperator = 2,

    /// <summary>Isolated organization workspace or verified custom domain.</summary>
    TenantWorkspace = 3,
}
