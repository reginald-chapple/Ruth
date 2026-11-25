using System.ComponentModel.DataAnnotations;

namespace Ruth.Modules.Identity.Slices.Authentication;

/// <summary>Request payload for completing an external OIDC authorization code callback.</summary>
public sealed record CompleteOidcLoginRequest
{
    /// <summary>Gets the anti-CSRF state returned by the external provider.</summary>
    [Required]
    public required string State { get; init; }

    /// <summary>Gets the authorization code returned by the external provider.</summary>
    [Required]
    public required string Code { get; init; }
}
