using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Pledges;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Pledges;

internal sealed class CancelFinancialPledgeCommandHandler(FinanceDbContext dbContext, IClock clock)
    : ICommandHandler<CancelFinancialPledgeCommand, FinancialPledgeResponse>
{
    public async Task<Result<FinancialPledgeResponse>> HandleAsync(
        CancelFinancialPledgeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var pledgeId = FinancialPledgeId.From(command.PledgeId);
        var pledge = await dbContext.FinancialPledges.FirstOrDefaultAsync(
            p => p.Id == pledgeId,
            cancellationToken
        );
        if (pledge is null)
        {
            return FinanceErrors.PledgeNotFound;
        }

        var cancelled = pledge.Cancel(command.Reason);
        if (cancelled.IsFailure)
        {
            return Result.Failure<FinancialPledgeResponse>(cancelled.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var accountName = await dbContext
            .FinancialAccounts.AsNoTracking()
            .Where(a => a.Id == pledge.AccountId)
            .Select(static a => a.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Account";

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        return FinancialPledgeMapper.ToResponse(pledge, accountName, 0m, today);
    }
}
