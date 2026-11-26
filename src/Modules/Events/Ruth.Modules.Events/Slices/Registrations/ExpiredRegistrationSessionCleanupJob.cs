using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class ExpiredRegistrationSessionCleanupJob(IServiceScopeFactory scopeFactory)
    : IScheduledJob
{
    public string JobKey => "events.expired-registration-session-cleanup";

    public string DefaultCronExpression => "*/5 * * * *";

    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var result = await dispatcher.SendAsync<
            ExpireRegistrationSessionsCommand,
            RegistrationSessionCleanupResultResponse
        >(new ExpireRegistrationSessionsCommand(), cancellationToken);

        if (result.IsFailure)
        {
            return JobExecutionResult.Failed(result.PrimaryError.Description);
        }

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"Expired {result.Value.ExpiredSessionCount} stale registration sessions; promoted {result.Value.AutoPromotedRegistrantCount} waitlisted registrants."
        );
        return JobExecutionResult.Success(summary);
    }
}
