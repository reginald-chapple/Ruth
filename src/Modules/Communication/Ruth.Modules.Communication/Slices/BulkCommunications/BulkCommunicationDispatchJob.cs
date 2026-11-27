using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;

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
        if (AmbientTenantContext.Instance.HasTenant && !AmbientTenantContext.Instance.IsLive)
        {
            return JobExecutionResult.Success("Skipped: tenant workspace is not Live.");
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var result = await dispatcher.SendAsync<
            ProcessDueBulkCommunicationsCommand,
            BulkDispatchBatchResultResponse
        >(new ProcessDueBulkCommunicationsCommand(), cancellationToken);

        return !result.IsFailure
                ? JobExecutionResult.Success(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Touched={result.Value.CommunicationsTouched}, Completed={result.Value.CommunicationsCompleted}, Delivered={result.Value.RecipientsDelivered}, Skipped={result.Value.RecipientsSkipped}, Failed={result.Value.RecipientsFailed}"
                    )
                )
            : string.Equals(
                result.PrimaryError.Code,
                BulkCommunicationErrors.TenantNotLive.Code,
                StringComparison.Ordinal
            )
                ? JobExecutionResult.Success("Skipped: tenant workspace is not Live.")
            : JobExecutionResult.Failed(result.PrimaryError.Description);
    }
}
