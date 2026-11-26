using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Invoices;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Invoices;

internal sealed class GetInvoiceByIdQueryHandler(FinanceDbContext dbContext, IClock clock)
    : IQueryHandler<GetInvoiceByIdQuery, InvoiceResponse>
{
    public async Task<Result<InvoiceResponse>> HandleAsync(
        GetInvoiceByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var invoiceId = InvoiceId.From(query.InvoiceId);
        var invoice = await dbContext
            .Invoices.AsNoTracking()
            .Include(static i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);
        if (invoice is null)
        {
            return FinanceErrors.InvoiceNotFound;
        }

        var accountIds = invoice.LineItems.Select(static l => l.AccountId).Distinct().ToArray();
        var names = await dbContext
            .FinancialAccounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(static a => a.Id, static a => a.Name, cancellationToken);
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        return InvoiceMapper.ToResponse(invoice, names, today);
    }
}
