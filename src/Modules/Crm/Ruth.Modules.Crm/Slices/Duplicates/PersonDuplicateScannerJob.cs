using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.SharedKernel.Jobs;

namespace Ruth.Modules.Crm.Slices.Duplicates;

/// <summary>Scheduled nightly background job scanning CRM person records for potential duplicates.</summary>
public sealed class PersonDuplicateScannerJob(IServiceScopeFactory scopeFactory) : IScheduledJob
{
    /// <summary>Stable job key registered with the PostgreSQL advisory-locked scheduler.</summary>
    public const string Key = "ruth.crm.person-duplicate-scanner";

    /// <inheritdoc />
    public string JobKey => Key;

    /// <inheritdoc />
    public string DefaultCronExpression => "0 3 * * *";

    /// <inheritdoc />
    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var scanner = scope.ServiceProvider.GetRequiredService<PersonDuplicateScannerService>();
        var summary = await scanner.ScanAsync(includeInactive: false, cancellationToken);

        return JobExecutionResult.Success(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Scanned {summary.PeopleScanned} people: {summary.PairsCreated} created, {summary.PairsUpdated} updated, {summary.StalePairsRemoved} pruned."
            )
        );
    }
}
