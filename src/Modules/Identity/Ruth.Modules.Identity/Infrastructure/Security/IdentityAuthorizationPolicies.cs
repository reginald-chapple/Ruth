namespace Ruth.Modules.Identity.Infrastructure.Security;

/// <summary>Authentication scheme and authorization policy constants for Ruth.</summary>
public static class IdentityAuthorizationPolicies
{
    /// <summary>Policy requiring membership in the system Administrators security role.</summary>
    public const string AdministratorsOnly = "Ruth.AdministratorsOnly";

    /// <summary>Authentication scheme for hashed REST API keys (Authorization-Token or X-Api-Key).</summary>
    public const string ApiKeyScheme = "RuthApiKey";

    /// <summary>Smart forwarding policy scheme selecting JWT Bearer, API Key, or Cookie authentication.</summary>
    public const string SmartScheme = "RuthSmartAuth";
}
