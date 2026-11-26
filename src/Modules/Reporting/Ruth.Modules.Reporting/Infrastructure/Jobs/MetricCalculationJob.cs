using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Infrastructure.Jobs;

internal sealed class MetricCalculationJob(IServiceScopeFactory scopeFactory) : IScheduledJob
{
    public string JobKey => "reporting.metrics.calculate";
    public string DefaultCronExpression => "0 * * * *";

    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var scheduledMetrics = await db
            .Metrics.AsNoTracking()
            .Where(static m => m.IsActive && m.SourceType != MetricSourceType.Manual)
            .Select(static m => m.Id.Value)
            .ToListAsync(cancellationToken);

        var calculated = 0;
        foreach (var metricId in scheduledMetrics)
        {
            var res = await commands.SendAsync<CalculateMetricCommand, MetricValueResponse>(
                new CalculateMetricCommand(metricId),
                cancellationToken
            );
            if (res.IsSuccess)
            {
                calculated++;
            }
        }

        return JobExecutionResult.Success(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Calculated {calculated} scheduled Metric(s)."
            )
        );
    }
}
