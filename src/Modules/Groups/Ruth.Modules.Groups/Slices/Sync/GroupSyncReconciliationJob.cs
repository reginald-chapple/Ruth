using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.SharedKernel.Jobs;

namespace Ruth.Modules.Groups.Slices.Sync;

/// <summary>Scheduled background job reconciling GroupSync rules against EntitySets, CRM cohorts, or source Groups.</summary>
public sealed class GroupSyncReconciliationJob(IServiceScopeFactory scopeFactory) : IScheduledJob
{
    /// <summary>Stable job key registered with the PostgreSQL advisory-locked scheduler.</summary>
    public const string Key = "ruth.groups.group-sync";

    /// <inheritdoc />
    public string JobKey => Key;

    /// <inheritdoc />
    public string DefaultCronExpression => "*/15 * * * *";

    /// <inheritdoc />
    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<GroupSyncReconciliationService>();
        var runs = await service.ReconcileAsync(ruleId: null, forceRun: false, cancellationToken);

        var added = runs.Sum(static r => r.MembersAdded);
        var removed = runs.Sum(static r => r.MembersRemoved);
        return JobExecutionResult.Success(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Executed {runs.Count} GroupSync rules: {added} added, {removed} removed."
            )
        );
    }
}
