using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Query to list all versions of HTML content for a CMS block.</summary>
public sealed record GetHtmlContentVersionsQuery(Guid BlockId, string? EntityValue = null)
    : IQuery<IReadOnlyList<HtmlContentResponse>>;
