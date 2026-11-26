using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Invoices;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Invoices;

internal sealed class CreateInvoiceCommandHandler(FinanceDbContext dbContext, IClock clock)
    : ICommandHandler<CreateInvoiceCommand, InvoiceResponse>
{
    public async Task<Result<InvoiceResponse>> HandleAsync(
        CreateInvoiceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.LineItems is null || command.LineItems.Count == 0)
        {
            return FinanceErrors.InvoiceLineItemsRequired;
        }

        var resolved = await ResolveLinesAsync(
            command.LineItems,
            command.IssueDate,
            cancellationToken
        );
        if (resolved.IsFailure)
        {
            return Result.Failure<InvoiceResponse>(resolved.Errors);
        }

        var created = Invoice.Create(
            command.PersonId,
            command.IssueDate,
            command.DueDate,
            command.CurrencyCode,
            resolved.Value.Specs,
            command.IssueImmediately,
            command.HouseholdId,
            command.BranchId,
            command.Memo
        );
        if (created.IsFailure)
        {
            return Result.Failure<InvoiceResponse>(created.Errors);
        }

        _ = dbContext.Invoices.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        return InvoiceMapper.ToResponse(created.Value, resolved.Value.AccountNames, today);
    }

    private async Task<
        Result<(
            List<(FinancialAccountId, string, decimal, decimal, bool)> Specs,
            Dictionary<FinancialAccountId, string> AccountNames
        )>
    > ResolveLinesAsync(
        IReadOnlyList<InvoiceLineItemSpec> lineItems,
        DateOnly issueDate,
        CancellationToken cancellationToken
    )
    {
        var requestedIds = lineItems
            .Select(static l => FinancialAccountId.From(l.AccountId))
            .Distinct()
            .ToArray();
        var accounts = await dbContext
            .FinancialAccounts.AsNoTracking()
            .Where(a => requestedIds.Contains(a.Id))
            .ToDictionaryAsync(static a => a.Id, cancellationToken);

        var resolvedSpecs = new List<(FinancialAccountId, string, decimal, decimal, bool)>(
            lineItems.Count
        );
        foreach (var line in lineItems)
        {
            var accountId = FinancialAccountId.From(line.AccountId);
            if (!accounts.TryGetValue(accountId, out var account))
            {
                return FinanceErrors.AccountNotFound;
            }

            if (!account.IsOpenOn(issueDate))
            {
                return FinanceErrors.AccountClosedForDate;
            }

            resolvedSpecs.Add(
                (
                    accountId,
                    line.Description,
                    line.Quantity,
                    line.UnitAmount,
                    account.IsTaxDeductible
                )
            );
        }

        var names = accounts.ToDictionary(static kv => kv.Key, static kv => kv.Value.Name);
        return (resolvedSpecs, names);
    }
}
