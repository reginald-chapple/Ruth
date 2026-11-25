using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Lists configured external OIDC authentication providers.</summary>
public sealed record GetExternalAuthProvidersQuery(bool EnabledOnly = false)
    : IQuery<IReadOnlyList<ExternalAuthProviderResponse>>;
