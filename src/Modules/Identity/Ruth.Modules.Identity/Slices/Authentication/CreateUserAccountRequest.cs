using System.ComponentModel.DataAnnotations;

namespace Ruth.Modules.Identity.Slices.Authentication;

/// <summary>Request payload for administrator provisioning of a new user account.</summary>
public sealed record CreateUserAccountRequest
{
    /// <summary>Gets the unique login username.</summary>
    [Required]
    public required string UserName { get; init; }

    /// <summary>Gets the user's email address.</summary>
    [Required]
    public required string Email { get; init; }

    /// <summary>Gets the display name.</summary>
    [Required]
    public required string DisplayName { get; init; }

    /// <summary>Gets the initial password.</summary>
    [Required]
    public required string Password { get; init; }

    /// <summary>Gets whether the account starts confirmed.</summary>
    public bool IsConfirmed { get; init; } = true;

    /// <summary>Gets optional initial security role IDs.</summary>
    public IReadOnlyList<Guid>? RoleIds { get; init; }
}
