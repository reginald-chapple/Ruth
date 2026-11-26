using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Workflow.Tests.Infrastructure;

internal static class WorkflowEngineAssertionHelper
{
    public static async Task ResumeAndVerifyCompletionAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        WorkflowDbContext wfDb,
        Guid workflowId
    )
    {
        var pendingBatch = await commands.SendAsync<
            ProcessPendingWorkflowsCommand,
            WorkflowBatchProcessResponse
        >(new ProcessPendingWorkflowsCommand());
        Assert.True(pendingBatch.IsSuccess);
        Assert.Equal(1, pendingBatch.Value.CompletedCount);

        var reloaded = await queries.QueryAsync<
            GetWorkflowInstanceByIdQuery,
            WorkflowInstanceResponse
        >(new GetWorkflowInstanceByIdQuery(workflowId));
        Assert.Equal(WorkflowInstanceStatus.Completed, reloaded.Value.Status);
        Assert.Equal("Onboarded", reloaded.Value.StateLabel);

        var outboxTypes = await wfDb
            .OutboxMessages.AsNoTracking()
            .Select(static o => o.EventType)
            .ToListAsync();
        Assert.Contains(outboxTypes, static t =>
            t.Contains("SystemNotificationRequestedIntegrationEvent", StringComparison.Ordinal)
        );
        Assert.Contains(outboxTypes, static t =>
            t.Contains("WorkflowCompletedIntegrationEvent", StringComparison.Ordinal)
        );
    }
}
