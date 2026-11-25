namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Represents an OAuth/OIDC client application registered with Ruth.</summary>
public sealed record AuthClientResponse(
    Guid Id,
    string Name,
    string ClientId,
    string RedirectUri,
    string PostLogoutRedirectUri,
    string AllowedScopes,
    string AllowedClaims,
    bool AllowUserApiAccess,
    int ScopeApprovalExpirationDays,
    bool IsActive
);
