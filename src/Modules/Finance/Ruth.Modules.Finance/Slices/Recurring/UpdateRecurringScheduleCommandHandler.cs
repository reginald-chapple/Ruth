using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Domain.Recurring;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Recurring;

internal sealed class UpdateRecurringScheduleCommandHandler(
    FinanceDbContext dbContext,
    RecurringGatewaySyncHelper syncHelper
) : ICommandHandler<UpdateRecurringScheduleCommand, RecurringScheduleResponse>
{
    public async Task<Result<RecurringScheduleResponse>> HandleAsync(
        UpdateRecurringScheduleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Splits is null || command.Splits.Count == 0)
        {
            return FinanceErrors.RecurringScheduleInvalid;
        }

        var scheduleId = RecurringScheduleTransactionId.From(command.ScheduleId);
        var schedule = await dbContext
            .RecurringSchedules.Include(static s => s.Splits)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
        if (schedule is null)
        {
            return FinanceErrors.RecurringScheduleNotFound;
        }

        var methodId = SavedPaymentMethodId.From(command.SavedPaymentMethodId);
        var loaded = await syncHelper.LoadActiveGatewayAndMethodAsync(
            schedule.FinancialGatewayId,
            methodId,
            cancellationToken
        );
        if (loaded.IsFailure)
        {
            return Result.Failure<RecurringScheduleResponse>(loaded.Errors);
        }

        var updated = schedule.UpdateSchedule(
            methodId,
            command.Frequency,
            command.NextPaymentDate,
            command.EndDate,
            [
                .. command.Splits.Select(static s =>
                    (FinancialAccountId.From(s.AccountId), s.Amount, s.Summary)
                ),
            ]
        );
        if (updated.IsFailure)
        {
            return Result.Failure<RecurringScheduleResponse>(updated.Errors);
        }

        var (gateway, provider, savedMethod) = loaded.Value;
        await SyncUpdatedScheduleAsync(
            provider,
            gateway.IsTestMode,
            savedMethod.CustomerVaultToken,
            schedule,
            cancellationToken
        );

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return await RecurringScheduleMapper.HydrateResponseAsync(
            dbContext,
            schedule,
            cancellationToken
        );
    }

    private static async Task SyncUpdatedScheduleAsync(
        IPaymentGateway provider,
        bool isTestMode,
        string vaultToken,
        RecurringScheduleTransaction schedule,
        CancellationToken cancellationToken
    )
    {
        var syncedRef = await RecurringGatewaySyncHelper.SyncSubscriptionAsync(
            provider,
            isTestMode,
            vaultToken,
            schedule,
            GatewaySubscriptionAction.CreateOrUpdate,
            cancellationToken
        );
        if (syncedRef.IsSuccess)
        {
            schedule.SetGatewaySubscriptionReference(syncedRef.Value);
        }
    }
}
