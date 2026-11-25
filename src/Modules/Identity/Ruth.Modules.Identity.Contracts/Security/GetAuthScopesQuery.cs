using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Lists registered OAuth/OIDC scopes.</summary>
public sealed record GetAuthScopesQuery() : IQuery<IReadOnlyList<AuthScopeResponse>>;
