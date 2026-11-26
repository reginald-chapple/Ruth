using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Recurring;

/// <summary>Account split belonging to a <see cref="RecurringScheduleTransaction" />.</summary>
public sealed class RecurringScheduleSplit : Entity<RecurringScheduleSplitId>
{
    private RecurringScheduleSplit() { }

    internal RecurringScheduleSplit(
        RecurringScheduleSplitId id,
        RecurringScheduleTransactionId scheduleId,
        FinancialAccountId accountId,
        decimal amount,
        string? summary
    )
        : base(id)
    {
        ScheduleId = scheduleId;
        AccountId = accountId;
        Amount = amount;
        Summary = summary;
    }

    public RecurringScheduleTransactionId ScheduleId { get; private set; }
    public FinancialAccountId AccountId { get; private set; }
    public decimal Amount { get; private set; }
    public string? Summary { get; private set; }
}
