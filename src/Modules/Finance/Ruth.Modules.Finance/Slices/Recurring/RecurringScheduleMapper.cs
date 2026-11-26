using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Recurring;
using Ruth.Modules.Finance.Infrastructure;

namespace Ruth.Modules.Finance.Slices.Recurring;

internal static class RecurringScheduleMapper
{
    public static RecurringScheduleResponse ToResponse(
        RecurringScheduleTransaction schedule,
        string maskedPaymentMethod,
        IReadOnlyDictionary<FinancialAccountId, string> accountNames
    )
    {
        IReadOnlyList<RecurringScheduleSplitResponse> splits =
        [
            .. schedule.Splits.Select(s => new RecurringScheduleSplitResponse(
                s.Id.Value,
                s.AccountId.Value,
                accountNames.TryGetValue(s.AccountId, out var name) ? name : "Account",
                s.Amount,
                s.Summary
            )),
        ];

        return new RecurringScheduleResponse(
            schedule.Id.Value,
            schedule.PersonId,
            schedule.HouseholdId,
            schedule.FinancialGatewayId.Value,
            schedule.SavedPaymentMethodId.Value,
            maskedPaymentMethod,
            schedule.GatewaySubscriptionReference,
            schedule.Purpose,
            schedule.Frequency,
            schedule.Status,
            schedule.CurrencyCode,
            schedule.TotalAmount,
            schedule.StartDate,
            schedule.EndDate,
            schedule.NextPaymentDate,
            schedule.LastProcessedAtUtc,
            schedule.ConsecutiveFailureCount,
            schedule.LastFailureReason,
            splits,
            schedule.CreatedOnUtc
        );
    }

    public static async Task<RecurringScheduleResponse> HydrateResponseAsync(
        FinanceDbContext dbContext,
        RecurringScheduleTransaction schedule,
        CancellationToken cancellationToken
    )
    {
        var methodDisplay = await dbContext
            .SavedPaymentMethods.AsNoTracking()
            .Where(m => m.Id == schedule.SavedPaymentMethodId)
            .Select(static m => m.DisplayName)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Vaulted Method";

        var accountIds = schedule.Splits.Select(static s => s.AccountId).Distinct().ToArray();
        var names = await dbContext
            .FinancialAccounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(static a => a.Id, static a => a.Name, cancellationToken);

        return ToResponse(schedule, methodDisplay, names);
    }
}
