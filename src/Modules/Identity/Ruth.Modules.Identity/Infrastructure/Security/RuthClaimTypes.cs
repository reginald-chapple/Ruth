namespace Ruth.Modules.Identity.Infrastructure.Security;

/// <summary>Custom claim types emitted by Ruth authentication tokens and cookies.</summary>
public static class RuthClaimTypes
{
    /// <summary>Security role UUID claim emitted for each active role assignment.</summary>
    public const string RoleId = "ruth_role_id";

    /// <summary>Display name claim.</summary>
    public const string DisplayName = "display_name";
}
