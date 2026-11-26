using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Slices.Recurring;

internal sealed class RecurringSettlementSyncJob(IServiceScopeFactory scopeFactory) : IScheduledJob
{
    public string JobKey => "finance.recurring-settlement-sync";

    public string DefaultCronExpression => "15 2 * * *";

    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var result = await dispatcher.SendAsync<
            ProcessDueRecurringSchedulesCommand,
            RecurringSettlementRunResponse
        >(new ProcessDueRecurringSchedulesCommand(), cancellationToken);

        if (result.IsFailure)
        {
            return JobExecutionResult.Failed(result.PrimaryError.Description);
        }

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"Evaluated {result.Value.EvaluatedCount} due recurring schedules; settled {result.Value.SettledCount}, failed {result.Value.FailedCount}."
        );
        return JobExecutionResult.Success(summary);
    }
}
