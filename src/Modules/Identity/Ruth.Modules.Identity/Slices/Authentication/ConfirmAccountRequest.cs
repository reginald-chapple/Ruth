using System.ComponentModel.DataAnnotations;

namespace Ruth.Modules.Identity.Slices.Authentication;

/// <summary>Request payload to confirm a user account using a single-use confirmation token.</summary>
public sealed record ConfirmAccountRequest
{
    /// <summary>Gets the single-use confirmation token.</summary>
    [Required]
    public required string Token { get; init; }
}
