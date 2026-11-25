namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Represents a single-use account action token issuance result.</summary>
public sealed record IssuedTokenResponse(bool Issued, string? Token, DateTimeOffset? ExpiresOnUtc);
