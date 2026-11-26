using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.Modules.Finance.Domain.Recurring;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Recurring;

internal sealed class GetRecurringSchedulesQueryHandler(FinanceDbContext dbContext)
    : IQueryHandler<GetRecurringSchedulesQuery, IReadOnlyList<RecurringScheduleResponse>>
{
    public async Task<Result<IReadOnlyList<RecurringScheduleResponse>>> HandleAsync(
        GetRecurringSchedulesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var baseQuery = dbContext.RecurringSchedules.AsNoTracking().Include(static s => s.Splits);

        var schedules = await ApplyFilters(baseQuery, query)
            .OrderBy(static s => s.NextPaymentDate)
            .ToListAsync(cancellationToken);

        if (schedules.Count == 0)
        {
            IReadOnlyList<RecurringScheduleResponse> empty = [];
            return Result.Success(empty);
        }

        var methodIds = schedules.Select(static s => s.SavedPaymentMethodId).Distinct().ToArray();
        var methodLabels = await dbContext
            .SavedPaymentMethods.AsNoTracking()
            .Where(m => methodIds.Contains(m.Id))
            .ToDictionaryAsync(static m => m.Id, static m => m.DisplayName, cancellationToken);

        var accountIds = schedules
            .SelectMany(static s => s.Splits.Select(static sp => sp.AccountId))
            .Distinct()
            .ToArray();
        var accountNames = await dbContext
            .FinancialAccounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(static a => a.Id, static a => a.Name, cancellationToken);

        IReadOnlyList<RecurringScheduleResponse> mapped =
        [
            .. schedules.Select(s =>
                RecurringScheduleMapper.ToResponse(
                    s,
                    methodLabels.GetValueOrDefault(s.SavedPaymentMethodId, "Vaulted Method"),
                    accountNames
                )
            ),
        ];
        return Result.Success(mapped);
    }

    private static IQueryable<RecurringScheduleTransaction> ApplyFilters(
        IQueryable<RecurringScheduleTransaction> source,
        GetRecurringSchedulesQuery query
    )
    {
        if (query.PersonId is { } personId && personId != Guid.Empty)
        {
            source = source.Where(s => s.PersonId == personId);
        }

        if (query.Status is { } status)
        {
            source = source.Where(s => s.Status == status);
        }

        if (query.Purpose is { } purpose)
        {
            source = source.Where(s => s.Purpose == purpose);
        }

        return source;
    }
}
