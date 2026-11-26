using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Command to add a host domain to a CMS site.</summary>
public sealed record AddSiteDomainCommand(Guid SiteId, string Domain, bool IsPrimary = false)
    : ICommand<CmsSiteResponse>;
