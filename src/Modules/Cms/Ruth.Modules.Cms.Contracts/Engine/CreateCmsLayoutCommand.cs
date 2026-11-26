using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Command to create a CMS layout with named zones inside a site.</summary>
public sealed record CreateCmsLayoutCommand(
    Guid SiteId,
    string Name,
    string Slug,
    string FileName = "FullWidth",
    string? Description = null,
    IReadOnlyList<string>? Zones = null
) : ICommand<CmsLayoutResponse>;
