using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.Flows;

/// <summary>Scheduled background job advancing due automated CommunicationFlow drip steps.</summary>
public sealed class CommunicationFlowAdvancementJob(IServiceScopeFactory scopeFactory)
    : IScheduledJob
{
    /// <inheritdoc />
    public string JobKey => "ruth.communication.flow-advancement";

    /// <inheritdoc />
    public string DefaultCronExpression => "* * * * *";

    /// <inheritdoc />
    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var result = await dispatcher.SendAsync<
            ProcessDueCommunicationFlowsCommand,
            FlowAdvancementBatchResultResponse
        >(new ProcessDueCommunicationFlowsCommand(), cancellationToken);

        return result.IsFailure
            ? JobExecutionResult.Failed(result.PrimaryError.Description)
            : JobExecutionResult.Success(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Evaluated={result.Value.RecipientsEvaluated}, Sent={result.Value.StepsDispatched}, SkippedSmart={result.Value.StepsSkippedSmartRule}, Completed={result.Value.RecipientsCompletedFlow}"
                )
            );
    }
}
