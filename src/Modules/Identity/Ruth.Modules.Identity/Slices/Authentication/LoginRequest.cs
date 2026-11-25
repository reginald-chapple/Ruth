using System.ComponentModel.DataAnnotations;

namespace Ruth.Modules.Identity.Slices.Authentication;

/// <summary>Request payload for username/email and password login.</summary>
public sealed record LoginRequest
{
    /// <summary>Gets the username or email address.</summary>
    [Required]
    public required string UserNameOrEmail { get; init; }

    /// <summary>Gets the plaintext password.</summary>
    [Required]
    public required string Password { get; init; }

    /// <summary>Gets a value indicating whether cookie sign-in should persist across browser restarts.</summary>
    public bool RememberMe { get; init; }
}
