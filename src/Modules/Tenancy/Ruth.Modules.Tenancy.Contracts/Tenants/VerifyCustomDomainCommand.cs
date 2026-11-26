using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Verifies DNS TXT ownership for a custom domain belonging to the active tenant.</summary>
/// <param name="DomainId">The tenant domain identifier to verify.</param>
/// <param name="ObservedTxtToken">The observed or simulated TXT token value confirming domain control.</param>
public sealed record VerifyCustomDomainCommand(Guid DomainId, string ObservedTxtToken)
    : ICommand<TenantDomainResponse>;
