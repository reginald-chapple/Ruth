using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Pledges;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Pledges;

internal sealed class CreateFinancialPledgeCommandHandler(FinanceDbContext dbContext, IClock clock)
    : ICommandHandler<CreateFinancialPledgeCommand, FinancialPledgeResponse>
{
    public async Task<Result<FinancialPledgeResponse>> HandleAsync(
        CreateFinancialPledgeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var accountId = FinancialAccountId.From(command.AccountId);
        var account = await dbContext
            .FinancialAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (account is null)
        {
            return FinanceErrors.AccountNotFound;
        }

        var created = FinancialPledge.Create(
            command.PersonId,
            accountId,
            command.TotalAmount,
            command.CurrencyCode,
            command.Frequency,
            command.StartDate,
            command.EndDate,
            command.HouseholdId,
            command.BranchId ?? account.BranchId,
            command.Notes
        );
        if (created.IsFailure)
        {
            return Result.Failure<FinancialPledgeResponse>(created.Errors);
        }

        _ = dbContext.FinancialPledges.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        return FinancialPledgeMapper.ToResponse(created.Value, account.Name, 0m, today);
    }
}
