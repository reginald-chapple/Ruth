using System.ComponentModel.DataAnnotations;

namespace Ruth.Modules.Identity.Slices.Authentication;

/// <summary>Request payload to confirm a password reset using a single-use token.</summary>
public sealed record ConfirmPasswordResetRequest
{
    /// <summary>Gets the single-use password reset token.</summary>
    [Required]
    public required string Token { get; init; }

    /// <summary>Gets the new password.</summary>
    [Required]
    public required string NewPassword { get; init; }
}
