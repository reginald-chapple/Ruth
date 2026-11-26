using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Actions;

internal sealed class GetWorkflowActionCatalogQueryHandler(IEnumerable<IWorkflowAction> actions)
    : IQueryHandler<GetWorkflowActionCatalogQuery, IReadOnlyList<WorkflowActionPluginDescriptor>>
{
    public Task<Result<IReadOnlyList<WorkflowActionPluginDescriptor>>> HandleAsync(
        GetWorkflowActionCatalogQuery query,
        CancellationToken cancellationToken = default
    )
    {
        IReadOnlyList<WorkflowActionPluginDescriptor> list =
        [
            .. actions
                .OrderBy(static a => a.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static a => a.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(static a => new WorkflowActionPluginDescriptor(
                    a.ActionKey,
                    a.DisplayName,
                    a.Category,
                    a.Description
                )),
        ];

        return Task.FromResult(Result.Success(list));
    }
}
