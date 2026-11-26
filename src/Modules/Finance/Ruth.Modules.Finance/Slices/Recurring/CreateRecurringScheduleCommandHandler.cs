using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Domain.Recurring;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Recurring;

internal sealed class CreateRecurringScheduleCommandHandler(
    FinanceDbContext dbContext,
    RecurringGatewaySyncHelper syncHelper
) : ICommandHandler<CreateRecurringScheduleCommand, RecurringScheduleResponse>
{
    public async Task<Result<RecurringScheduleResponse>> HandleAsync(
        CreateRecurringScheduleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Splits is null || command.Splits.Count == 0)
        {
            return FinanceErrors.RecurringScheduleInvalid;
        }

        var gatewayId = FinancialGatewayId.From(command.FinancialGatewayId);
        var methodId = SavedPaymentMethodId.From(command.SavedPaymentMethodId);
        var loaded = await syncHelper.LoadActiveGatewayAndMethodAsync(
            gatewayId,
            methodId,
            cancellationToken
        );
        if (loaded.IsFailure)
        {
            return Result.Failure<RecurringScheduleResponse>(loaded.Errors);
        }

        var accounts = await LoadActiveAccountNamesAsync(command.Splits, cancellationToken);
        if (accounts is null)
        {
            return FinanceErrors.AccountNotFound;
        }

        var created = CreateDomainSchedule(command, gatewayId, methodId);
        if (created.IsFailure)
        {
            return Result.Failure<RecurringScheduleResponse>(created.Errors);
        }

        var (gateway, provider, savedMethod) = loaded.Value;
        var syncedRef = await RecurringGatewaySyncHelper.SyncSubscriptionAsync(
            provider,
            gateway.IsTestMode,
            savedMethod.CustomerVaultToken,
            created.Value,
            GatewaySubscriptionAction.CreateOrUpdate,
            cancellationToken
        );
        if (syncedRef.IsFailure)
        {
            return Result.Failure<RecurringScheduleResponse>(syncedRef.Errors);
        }

        created.Value.SetGatewaySubscriptionReference(syncedRef.Value);
        _ = dbContext.RecurringSchedules.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return RecurringScheduleMapper.ToResponse(created.Value, savedMethod.DisplayName, accounts);
    }

    private async Task<Dictionary<FinancialAccountId, string>?> LoadActiveAccountNamesAsync(
        IReadOnlyList<RecurringScheduleSplitSpec> splits,
        CancellationToken cancellationToken
    )
    {
        var accountIds = splits
            .Select(static s => FinancialAccountId.From(s.AccountId))
            .Distinct()
            .ToArray();
        var accounts = await dbContext
            .FinancialAccounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.Id) && a.IsActive)
            .ToDictionaryAsync(static a => a.Id, static a => a.Name, cancellationToken);

        return accounts.Count == accountIds.Length ? accounts : null;
    }

    private static Result<RecurringScheduleTransaction> CreateDomainSchedule(
        CreateRecurringScheduleCommand command,
        FinancialGatewayId gatewayId,
        SavedPaymentMethodId methodId
    )
    {
        return RecurringScheduleTransaction.Create(
            command.PersonId,
            gatewayId,
            methodId,
            command.Purpose,
            command.Frequency,
            command.CurrencyCode,
            command.StartDate,
            command.EndDate,
            [
                .. command.Splits.Select(static s =>
                    (FinancialAccountId.From(s.AccountId), s.Amount, s.Summary)
                ),
            ],
            command.HouseholdId
        );
    }
}
