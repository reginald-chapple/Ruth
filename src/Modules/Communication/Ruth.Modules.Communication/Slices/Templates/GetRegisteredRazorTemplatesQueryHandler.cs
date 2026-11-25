using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Templates;

internal sealed class GetRegisteredRazorTemplatesQueryHandler(IRazorTemplateRegistry registry)
    : IQueryHandler<GetRegisteredRazorTemplatesQuery, IReadOnlyList<RazorTemplateSummaryResponse>>
{
    public Task<Result<IReadOnlyList<RazorTemplateSummaryResponse>>> HandleAsync(
        GetRegisteredRazorTemplatesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<RazorTemplateDescriptor> items = registry.GetAll();
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = query.Category.Trim();
            items = items.Where(d =>
                string.Equals(d.Category, category, StringComparison.OrdinalIgnoreCase)
            );
        }

        IReadOnlyList<RazorTemplateSummaryResponse> response =
        [
            .. items.Select(TemplateSnippetMapper.ToSummary),
        ];
        return Task.FromResult(Result.Success(response));
    }
}
