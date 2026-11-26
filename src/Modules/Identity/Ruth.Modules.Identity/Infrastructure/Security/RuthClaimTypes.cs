namespace Ruth.Modules.Identity.Infrastructure.Security;

/// <summary>Custom claim types emitted by authentication tokens and session cookies.</summary>
public static class RuthClaimTypes
{
    /// <summary>Security role UUID claim emitted for each active role assignment.</summary>
    public const string RoleId = "role_id";

    /// <summary>Display name claim.</summary>
    public const string DisplayName = "display_name";

    /// <summary>Immutable tenant UUID claim binding tokens and cookies to a single organization.</summary>
    public const string TenantId = "tid";

    /// <summary>Platform operator marker claim valid strictly on the operator control-plane host.</summary>
    public const string PlatformOperator = "platform_operator";
}
