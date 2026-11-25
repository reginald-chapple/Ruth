using System.ComponentModel.DataAnnotations;

namespace Ruth.Modules.Identity.Slices.Security;

/// <summary>Request payload for registering a new OAuth/OIDC client.</summary>
public sealed record CreateAuthClientRequest
{
    /// <summary>Gets the client display name.</summary>
    [Required]
    public required string Name { get; init; }

    /// <summary>Gets the unique client ID.</summary>
    [Required]
    public required string ClientId { get; init; }

    /// <summary>Gets the optional client secret.</summary>
    public string? ClientSecret { get; init; }

    /// <summary>Gets the allowed redirect URI.</summary>
    [Required]
    public required string RedirectUri { get; init; }

    /// <summary>Gets the allowed post-logout redirect URI.</summary>
    [Required]
    public required string PostLogoutRedirectUri { get; init; }

    /// <summary>Gets the space-delimited allowed scopes.</summary>
    public string AllowedScopes { get; init; } = "openid profile email";

    /// <summary>Gets the comma-delimited allowed claims.</summary>
    public string AllowedClaims { get; init; } = string.Empty;

    /// <summary>Gets whether the client may access user APIs.</summary>
    public bool AllowUserApiAccess { get; init; }

    /// <summary>Gets the scope approval expiration in days.</summary>
    public int ScopeApprovalExpirationDays { get; init; } = 365;
}
