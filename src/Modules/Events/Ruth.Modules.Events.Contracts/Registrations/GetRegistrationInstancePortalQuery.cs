using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Query to bootstrap the public multi-step Registration Wizard by instance slug or GUID.</summary>
public sealed record GetRegistrationInstancePortalQuery(string InstanceIdOrSlug)
    : IQuery<PublicRegistrationWizardInitResponse>;
