using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Engagement.Contracts.Following;
using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Slices.Reminders;

internal sealed class ReminderAndFollowingDigestJob(IServiceScopeFactory scopeFactory)
    : IScheduledJob
{
    public string JobKey => "engagement.reminders-and-following-digest";
    public string DefaultCronExpression => "30 * * * *";

    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var rem = await dispatcher.SendAsync<
            ProcessDueRemindersCommand,
            ReminderProcessRunResponse
        >(new ProcessDueRemindersCommand(), cancellationToken);
        if (rem.IsFailure)
        {
            return JobExecutionResult.Failed(rem.PrimaryError.Description);
        }

        var dig = await dispatcher.SendAsync<
            DispatchFollowingDigestsCommand,
            FollowingDigestRunResponse
        >(new DispatchFollowingDigestsCommand(), cancellationToken);
        return dig.IsFailure
            ? JobExecutionResult.Failed(dig.PrimaryError.Description)
            : JobExecutionResult.Success(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Notified {rem.Value.NotifiedCount} reminders and {dig.Value.DigestsSentCount} following digests."
                )
            );
    }
}
