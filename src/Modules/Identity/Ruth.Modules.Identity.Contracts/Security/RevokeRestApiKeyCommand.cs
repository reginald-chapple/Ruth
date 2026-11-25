using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Revokes an active REST API key.</summary>
public sealed record RevokeRestApiKeyCommand(Guid RestApiKeyId) : ICommand<bool>;
