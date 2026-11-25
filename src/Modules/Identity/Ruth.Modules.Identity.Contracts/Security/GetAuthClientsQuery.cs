using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Lists registered OAuth/OIDC clients.</summary>
public sealed record GetAuthClientsQuery() : IQuery<IReadOnlyList<AuthClientResponse>>;
