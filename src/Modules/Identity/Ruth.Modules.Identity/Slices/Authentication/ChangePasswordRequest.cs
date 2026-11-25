using System.ComponentModel.DataAnnotations;

namespace Ruth.Modules.Identity.Slices.Authentication;

/// <summary>Request payload for changing the current user's password.</summary>
public sealed record ChangePasswordRequest
{
    /// <summary>Gets the user's current password.</summary>
    [Required]
    public required string CurrentPassword { get; init; }

    /// <summary>Gets the desired new password.</summary>
    [Required]
    public required string NewPassword { get; init; }
}
