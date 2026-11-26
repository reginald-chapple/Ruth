using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Reporting.Infrastructure.Jobs;

internal sealed class DataViewMaterializerJob(IServiceScopeFactory scopeFactory, IClock clock)
    : IScheduledJob
{
    public string JobKey => "reporting.dataviews.materialize";
    public string DefaultCronExpression => "*/15 * * * *";

    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var nowUtc = clock.UtcNow;

        var candidates = await db
            .DataViews.AsNoTracking()
            .Where(static d => d.IsActive && d.PersistedScheduleIntervalMinutes != null)
            .ToListAsync(cancellationToken);

        var refreshed = 0;
        foreach (var dv in candidates)
        {
            if (dv.IsPersistedFreshAt(nowUtc))
            {
                continue;
            }

            var res = await commands.SendAsync<
                MaterializeDataViewCommand,
                DataViewExecutionResponse
            >(new MaterializeDataViewCommand(dv.Id.Value), cancellationToken);
            if (res.IsSuccess)
            {
                refreshed++;
            }
        }

        return JobExecutionResult.Success(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Materialized {refreshed} stale DataView(s)."
            )
        );
    }
}
