using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Exchanges a valid refresh token for a rotated refresh token and new JWT access token.</summary>
public sealed record RefreshAccessTokenCommand(
    string RefreshToken,
    string? IpAddress = null,
    string? UserAgent = null
) : ICommand<AuthTokensResponse>;
