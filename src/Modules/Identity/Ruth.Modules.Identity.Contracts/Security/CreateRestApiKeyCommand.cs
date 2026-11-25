using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Generates a new SHA-256-hashed REST API key bound to a user account.</summary>
public sealed record CreateRestApiKeyCommand(
    string Name,
    string? Description,
    Guid UserAccountId,
    DateTimeOffset? ExpiresOnUtc = null
) : ICommand<CreateRestApiKeyResponse>;
