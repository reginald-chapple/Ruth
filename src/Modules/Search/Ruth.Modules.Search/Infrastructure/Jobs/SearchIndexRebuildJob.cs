using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Search.Infrastructure.Jobs;

internal sealed class SearchIndexRebuildJob(IServiceScopeFactory scopeFactory) : IScheduledJob
{
    public string JobKey => "search.index.rebuild";
    public string DefaultCronExpression => "*/30 * * * *";

    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var res = await commands.SendAsync<RebuildSearchIndexCommand, SearchIndexRebuildResponse>(
            new RebuildSearchIndexCommand(),
            cancellationToken
        );

        return res.IsSuccess
            ? JobExecutionResult.Success(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Reindexed {res.Value.DocumentsUpserted} document(s) across {res.Value.ProvidersExecuted} provider(s)."
                )
            )
            : JobExecutionResult.Failed(res.PrimaryError.Description);
    }
}
