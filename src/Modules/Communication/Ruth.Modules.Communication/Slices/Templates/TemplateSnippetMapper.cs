using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Domain.Templates;

namespace Ruth.Modules.Communication.Slices.Templates;

internal static class TemplateSnippetMapper
{
    public static TemplateSnippetResponse ToResponse(TemplateSnippet snippet)
    {
        return new TemplateSnippetResponse(
            snippet.Id.Value,
            snippet.SystemGuid,
            snippet.Name,
            snippet.Key,
            snippet.Description,
            snippet.Documentation,
            snippet.Markup,
            snippet.OutputMode,
            new Dictionary<string, string>(
                snippet.DefaultParameters,
                StringComparer.OrdinalIgnoreCase
            ),
            [.. snippet.CategoryIds],
            snippet.IsSystem,
            snippet.IsActive,
            snippet.CreatedOnUtc,
            snippet.ModifiedOnUtc
        );
    }

    public static RazorTemplateSummaryResponse ToSummary(RazorTemplateDescriptor descriptor)
    {
        return new RazorTemplateSummaryResponse(
            descriptor.Key,
            descriptor.DisplayName,
            descriptor.Description,
            descriptor.Category,
            descriptor.ComponentType.Name,
            descriptor.Parameters
        );
    }
}
