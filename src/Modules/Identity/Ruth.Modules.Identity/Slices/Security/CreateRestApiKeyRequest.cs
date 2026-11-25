using System.ComponentModel.DataAnnotations;

namespace Ruth.Modules.Identity.Slices.Security;

/// <summary>Request payload for generating a new REST API key.</summary>
public sealed record CreateRestApiKeyRequest
{
    /// <summary>Gets the friendly key label.</summary>
    [Required]
    public required string Name { get; init; }

    /// <summary>Gets the optional key description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the target user account identifier.</summary>
    public Guid UserAccountId { get; init; }

    /// <summary>Gets the optional UTC expiration timestamp.</summary>
    public DateTimeOffset? ExpiresOnUtc { get; init; }
}
