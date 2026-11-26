using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

/// <summary>Scheduled background job dispatching due approved bulk Communication recipient batches.</summary>
public sealed class BulkCommunicationDispatchJob(IServiceScopeFactory scopeFactory) : IScheduledJob
{
    /// <inheritdoc />
    public string JobKey => "ruth.communication.bulk-dispatch";

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
            ProcessDueBulkCommunicationsCommand,
            BulkDispatchBatchResultResponse
        >(new ProcessDueBulkCommunicationsCommand(), cancellationToken);

        return result.IsFailure
            ? JobExecutionResult.Failed(result.PrimaryError.Description)
            : JobExecutionResult.Success(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Touched={result.Value.CommunicationsTouched}, Completed={result.Value.CommunicationsCompleted}, Delivered={result.Value.RecipientsDelivered}, Skipped={result.Value.RecipientsSkipped}, Failed={result.Value.RecipientsFailed}"
                )
            );
    }
}
