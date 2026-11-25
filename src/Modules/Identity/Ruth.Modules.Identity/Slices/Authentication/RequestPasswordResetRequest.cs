using System.ComponentModel.DataAnnotations;

namespace Ruth.Modules.Identity.Slices.Authentication;

/// <summary>Request payload to initiate a password reset.</summary>
public sealed record RequestPasswordResetRequest
{
    /// <summary>Gets the email address or username of the account.</summary>
    [Required]
    public required string EmailOrUserName { get; init; }
}
