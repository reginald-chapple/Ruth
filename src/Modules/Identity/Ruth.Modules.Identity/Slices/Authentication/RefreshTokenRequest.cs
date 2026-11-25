using System.ComponentModel.DataAnnotations;

namespace Ruth.Modules.Identity.Slices.Authentication;

/// <summary>Request payload for refreshing a JWT access token.</summary>
public sealed record RefreshTokenRequest
{
    /// <summary>Gets the active refresh token.</summary>
    [Required]
    public required string RefreshToken { get; init; }
}
