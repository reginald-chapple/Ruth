using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class ExpiredBookingSlotHoldCleanupJob(IServiceScopeFactory scopeFactory)
    : IScheduledJob
{
    public string JobKey => "commerce.expired-booking-slot-hold-cleanup";
    public string DefaultCronExpression => "*/5 * * * *";

    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var res = await dispatcher.SendAsync<
            ExpireBookingSlotHoldsCommand,
            BookingSlotHoldCleanupResponse
        >(new ExpireBookingSlotHoldsCommand(), cancellationToken);

        return res.IsFailure
            ? JobExecutionResult.Failed(res.PrimaryError.Description)
            : JobExecutionResult.Success(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Released {res.Value.ExpiredHoldCount} expired booking slot holds."
                )
            );
    }
}
