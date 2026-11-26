using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Slices.CadenceAlerts;

internal sealed class TransactionCadenceScannerJob(IServiceScopeFactory scopeFactory)
    : IScheduledJob
{
    public string JobKey => "finance.cadence-alert-scan";

    public string DefaultCronExpression => "45 2 * * *";

    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var result = await dispatcher.SendAsync<
            RunTransactionCadenceScanCommand,
            CadenceScanSummaryResponse
        >(new RunTransactionCadenceScanCommand(), cancellationToken);

        if (result.IsFailure)
        {
            return JobExecutionResult.Failed(result.PrimaryError.Description);
        }

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"Evaluated {result.Value.EvaluatedConstituentCount} constituents; emitted {result.Value.AlertsCreatedCount} cadence/spike alerts."
        );
        return JobExecutionResult.Success(summary);
    }
}
