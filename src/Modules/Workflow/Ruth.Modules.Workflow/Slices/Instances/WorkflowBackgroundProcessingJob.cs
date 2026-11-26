using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Slices.Instances;

internal sealed class WorkflowBackgroundProcessingJob(IServiceScopeFactory scopeFactory)
    : IScheduledJob
{
    public string JobKey => "workflow.background-instance-processor";
    public string DefaultCronExpression => "*/1 * * * *";

    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var res = await dispatcher.SendAsync<
            ProcessPendingWorkflowsCommand,
            WorkflowBatchProcessResponse
        >(new ProcessPendingWorkflowsCommand(MaxBatchSize: 50), cancellationToken);

        return res.IsFailure
            ? JobExecutionResult.Failed(res.PrimaryError.Description)
            : JobExecutionResult.Success(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Processed {res.Value.EvaluatedCount} workflows (completed={res.Value.CompletedCount}, waiting={res.Value.WaitingCount}, faulted={res.Value.FaultedCount})."
                )
            );
    }
}
