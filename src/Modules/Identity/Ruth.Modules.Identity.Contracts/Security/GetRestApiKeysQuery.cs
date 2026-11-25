using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Lists REST API keys, optionally filtered to a specific user account.</summary>
public sealed record GetRestApiKeysQuery(Guid? UserAccountId = null)
    : IQuery<IReadOnlyList<RestApiKeyResponse>>;
