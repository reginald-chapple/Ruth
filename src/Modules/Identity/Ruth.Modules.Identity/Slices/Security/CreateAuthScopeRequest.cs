using System.ComponentModel.DataAnnotations;

namespace Ruth.Modules.Identity.Slices.Security;

/// <summary>Request payload for creating an OAuth/OIDC scope.</summary>
public sealed record CreateAuthScopeRequest
{
    /// <summary>Gets the scope key.</summary>
    [Required]
    public required string Name { get; init; }

    /// <summary>Gets the user-facing display name.</summary>
    [Required]
    public required string PublicName { get; init; }
}
