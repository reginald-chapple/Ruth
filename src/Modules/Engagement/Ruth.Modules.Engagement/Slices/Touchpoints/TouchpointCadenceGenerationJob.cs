using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Slices.Touchpoints;

internal sealed class TouchpointCadenceGenerationJob(IServiceScopeFactory scopeFactory)
    : IScheduledJob
{
    public string JobKey => "engagement.touchpoint-cadence-generator";
    public string DefaultCronExpression => "0 5 * * *";

    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var res = await dispatcher.SendAsync<
            GeneratePortfolioCadenceTouchpointsCommand,
            TouchpointCadenceRunResponse
        >(new GeneratePortfolioCadenceTouchpointsCommand(), cancellationToken);

        return res.IsFailure
            ? JobExecutionResult.Failed(res.PrimaryError.Description)
            : JobExecutionResult.Success(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Generated {res.Value.GeneratedTouchpointCount} touchpoints across {res.Value.RefreshedContactCount} portfolio contacts."
                )
            );
    }
}
