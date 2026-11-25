using System.ComponentModel.DataAnnotations;

namespace Ruth.Modules.Identity.Slices.Security;

/// <summary>Request payload for creating a custom security role.</summary>
public sealed record CreateSecurityRoleRequest
{
    /// <summary>Gets the role display name.</summary>
    [Required]
    public required string Name { get; init; }

    /// <summary>Gets the optional role description.</summary>
    public string? Description { get; init; }
}
