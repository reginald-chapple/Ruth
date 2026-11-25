using System.ComponentModel.DataAnnotations;

namespace Ruth.Modules.Identity.Slices.Authentication;

/// <summary>Request payload for creating or updating an external OIDC provider configuration.</summary>
public sealed record UpsertExternalAuthProviderRequest
{
    /// <summary>Gets the unique lowercase provider key.</summary>
    [Required]
    public required string ProviderKey { get; init; }

    /// <summary>Gets the human-readable display name.</summary>
    [Required]
    public required string DisplayName { get; init; }

    /// <summary>Gets the OIDC authority URL.</summary>
    [Required]
    public required string Authority { get; init; }

    /// <summary>Gets the OIDC client ID.</summary>
    [Required]
    public required string ClientId { get; init; }

    /// <summary>Gets the optional OIDC client secret.</summary>
    public string? ClientSecret { get; init; }

    /// <summary>Gets the space-delimited OIDC scopes.</summary>
    public string Scopes { get; init; } = "openid profile email";

    /// <summary>Gets whether the provider is enabled.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Gets whether verified IdP email addresses may auto-link to existing accounts.</summary>
    public bool AutoLinkByEmail { get; init; }

    /// <summary>Gets the callback URI path.</summary>
    [Required]
    public required string CallbackPath { get; init; }
}
