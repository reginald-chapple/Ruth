using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Verifies DNS TXT ownership for a custom domain belonging to the active tenant.</summary>
/// <param name="DomainId">The tenant domain identifier to verify.</param>
/// <param name="ObservedTxtToken">Optional simulated TXT token value honored strictly when DNS mock mode is enabled.</param>
public sealed record VerifyCustomDomainCommand(Guid DomainId, string? ObservedTxtToken = null)
    : ICommand<TenantDomainResponse>;
