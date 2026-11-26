using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Cms.Contracts.Datasets;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Infrastructure.Datasets;

internal sealed class PersistedDatasetRefreshJob(IServiceScopeFactory scopeFactory, IClock clock)
    : IScheduledJob
{
    public string JobKey => "cms.persisted-datasets.refresh";
    public string DefaultCronExpression => "*/15 * * * *";

    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var nowUtc = clock.UtcNow;

        var activeDatasets = await db
            .PersistedDatasets.AsNoTracking()
            .Where(static d => d.IsActive)
            .ToListAsync(cancellationToken);

        var refreshed = 0;
        foreach (var dataset in activeDatasets)
        {
            if (dataset.ExpireDateTimeUtc is not null && dataset.ExpireDateTimeUtc <= nowUtc)
            {
                continue;
            }

            if (!dataset.IsStaleAt(nowUtc))
            {
                continue;
            }

            var res = await commands.SendAsync<
                RefreshPersistedDatasetCommand,
                PersistedDatasetResponse
            >(new RefreshPersistedDatasetCommand(dataset.AccessKey), cancellationToken);
            if (res.IsSuccess)
            {
                refreshed++;
            }
        }

        return JobExecutionResult.Success(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Refreshed {refreshed} stale CMS persisted dataset(s)."
            )
        );
    }
}
