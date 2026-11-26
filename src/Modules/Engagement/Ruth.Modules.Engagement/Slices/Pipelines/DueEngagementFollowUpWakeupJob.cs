using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class DueEngagementFollowUpWakeupJob(IServiceScopeFactory scopeFactory)
    : IScheduledJob
{
    public string JobKey => "engagement.due-followup-wakeup";
    public string DefaultCronExpression => "0 * * * *";

    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var res = await dispatcher.SendAsync<
            WakeDueEngagementFollowUpsCommand,
            WakeupFollowUpsResponse
        >(new WakeDueEngagementFollowUpsCommand(), cancellationToken);

        return res.IsFailure
            ? JobExecutionResult.Failed(res.PrimaryError.Description)
            : JobExecutionResult.Success(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Woke {res.Value.WokenCount} due engagement follow-up requests."
                )
            );
    }
}
