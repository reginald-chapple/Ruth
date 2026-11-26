using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Search.Infrastructure.Providers;

internal sealed class WorkflowTypesSearchDocumentProvider(IServiceProvider serviceProvider)
    : ISearchDocumentProvider
{
    public string ProviderKey => "workflow.types";
    public string ModuleName => "Workflow";
    public string EntityTypeKey => "Ruth.Workflow.WorkflowType";

    public async Task<IReadOnlyList<SearchDocumentUpsertSpec>> LoadDocumentsAsync(
        SearchProviderLoadRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var handler = serviceProvider.GetService<
            IQueryHandler<GetWorkflowTypesQuery, IReadOnlyList<WorkflowTypeResponse>>
        >();
        if (handler is null)
        {
            return [];
        }

        var res = await handler.HandleAsync(
            new GetWorkflowTypesQuery(IncludeInactive: false),
            cancellationToken
        );
        return res.IsFailure
            ? []
            :
            [
                .. res
                    .Value.Take(Math.Clamp(request.MaxDocuments, 1, 500))
                    .Select(w => new SearchDocumentUpsertSpec(
                        ProviderKey,
                        ModuleName,
                        EntityTypeKey,
                        w.Id,
                        w.Name,
                        Subtitle: w.WorkTerm,
                        Summary: w.Description,
                        BodyText: $"{w.Name} {w.Slug} {w.WorkTerm} {w.Description}",
                        Tags: [w.Slug, w.WorkflowIdPrefix],
                        UrlPath: "/workflow/definitions",
                        IsPublic: false,
                        ContentCollectionId: request.ContentCollectionId
                    )),
            ];
    }
}
