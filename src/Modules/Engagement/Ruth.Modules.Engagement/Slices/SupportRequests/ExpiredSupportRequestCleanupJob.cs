using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Slices.SupportRequests;

internal sealed class ExpiredSupportRequestCleanupJob(IServiceScopeFactory scopeFactory)
    : IScheduledJob
{
    public string JobKey => "engagement.expired-support-request-cleanup";
    public string DefaultCronExpression => "15 * * * *";

    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var res = await dispatcher.SendAsync<
            ExpireSupportRequestsCommand,
            SupportExpirationCleanupResponse
        >(new ExpireSupportRequestsCommand(), cancellationToken);

        return res.IsFailure
            ? JobExecutionResult.Failed(res.PrimaryError.Description)
            : JobExecutionResult.Success(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Archived {res.Value.ArchivedCount} expired support requests."
                )
            );
    }
}
