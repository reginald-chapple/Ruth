using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Registers a white-label custom domain and generates a DNS TXT ownership verification token for the active tenant.</summary>
/// <param name="Hostname">The custom hostname owned by the organization (for example portal.helpinghands.org).</param>
/// <param name="MakePrimaryWhenVerified">Whether to promote this hostname to primary once verified and live.</param>
public sealed record RegisterCustomDomainCommand(string Hostname, bool MakePrimaryWhenVerified)
    : ICommand<TenantDomainResponse>;
