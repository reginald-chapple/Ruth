using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Authenticates a user account with username/email and password credentials.</summary>
public sealed record LoginCommand(
    string UserNameOrEmail,
    string Password,
    string? IpAddress = null,
    string? UserAgent = null
) : ICommand<AuthTokensResponse>;
