using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Infrastructure.Jobs;

internal sealed class StarSchemaRefreshJob(IServiceScopeFactory scopeFactory) : IScheduledJob
{
    public string JobKey => "reporting.star-schema.refresh";
    public string DefaultCronExpression => "30 2 * * *";

    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var res = await commands.SendAsync<
            RefreshStarSchemaProjectionsCommand,
            StarSchemaRefreshResponse
        >(new RefreshStarSchemaProjectionsCommand(), cancellationToken);

        return res.IsSuccess
            ? JobExecutionResult.Success(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Refreshed BI star schema ({res.Value.FactMetricDailyRows} metric facts, {res.Value.FactRevenueMonthlyRows} revenue facts, {res.Value.FactAttendanceWeeklyRows} attendance facts)."
                )
            )
            : JobExecutionResult.Failed(res.PrimaryError.Description);
    }
}
