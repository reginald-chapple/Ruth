using System.ComponentModel.DataAnnotations;

namespace Ruth.Modules.Identity.Slices.Security;

/// <summary>Request payload for updating a security role.</summary>
public sealed record UpdateSecurityRoleRequest
{
    /// <summary>Gets the role display name.</summary>
    [Required]
    public required string Name { get; init; }

    /// <summary>Gets the optional role description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets whether the role is active.</summary>
    public bool IsActive { get; init; } = true;
}
