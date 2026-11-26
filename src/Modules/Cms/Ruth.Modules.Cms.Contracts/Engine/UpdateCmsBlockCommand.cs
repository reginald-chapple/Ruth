using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Command to update an existing CMS block's zone, order, wrapper HTML, or settings.</summary>
public sealed record UpdateCmsBlockCommand(
    Guid BlockId,
    string Zone,
    string Name,
    int Order,
    bool IsActive = true,
    string? PreHtml = null,
    string? PostHtml = null,
    string? CssClass = null,
    IReadOnlyDictionary<string, string?>? Settings = null
) : ICommand<CmsBlockResponse>;
