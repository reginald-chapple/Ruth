using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Invoices;

internal sealed class GetInvoicesQueryHandler(FinanceDbContext dbContext, IClock clock)
    : IQueryHandler<GetInvoicesQuery, IReadOnlyList<InvoiceResponse>>
{
    public async Task<Result<IReadOnlyList<InvoiceResponse>>> HandleAsync(
        GetInvoicesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var invoicesQuery = dbContext
            .Invoices.AsNoTracking()
            .Include(static i => i.LineItems)
            .AsQueryable();

        if (query.PersonId is { } personId && personId != Guid.Empty)
        {
            invoicesQuery = invoicesQuery.Where(i => i.PersonId == personId);
        }

        if (query.Status is { } status)
        {
            invoicesQuery = invoicesQuery.Where(i => i.Status == status);
        }

        if (query.BranchId is { } branchId && branchId != Guid.Empty)
        {
            invoicesQuery = invoicesQuery.Where(i => i.BranchId == branchId);
        }

        var invoices = await invoicesQuery
            .OrderByDescending(static i => i.IssueDate)
            .ToListAsync(cancellationToken);

        if (invoices.Count == 0)
        {
            IReadOnlyList<InvoiceResponse> empty = [];
            return Result.Success(empty);
        }

        var accountIds = invoices
            .SelectMany(static i => i.LineItems.Select(static l => l.AccountId))
            .Distinct()
            .ToArray();
        var names = await dbContext
            .FinancialAccounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(static a => a.Id, static a => a.Name, cancellationToken);
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        IReadOnlyList<InvoiceResponse> mapped =
        [
            .. invoices.Select(inv => InvoiceMapper.ToResponse(inv, names, today)),
        ];
        return Result.Success(mapped);
    }
}
