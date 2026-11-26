using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Pledges;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Pledges;

internal sealed class UpdateFinancialPledgeCommandHandler(
    FinanceDbContext dbContext,
    IFinanceLedgerSummaryReader ledgerSummaryReader,
    IClock clock
) : ICommandHandler<UpdateFinancialPledgeCommand, FinancialPledgeResponse>
{
    public async Task<Result<FinancialPledgeResponse>> HandleAsync(
        UpdateFinancialPledgeCommand command,
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

        var revised = pledge.Revise(
            command.TotalAmount,
            command.Frequency,
            command.StartDate,
            command.EndDate,
            command.Notes
        );
        if (revised.IsFailure)
        {
            return Result.Failure<FinancialPledgeResponse>(revised.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var accountName = await dbContext
            .FinancialAccounts.AsNoTracking()
            .Where(a => a.Id == pledge.AccountId)
            .Select(static a => a.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Account";

        var settledMap = await ledgerSummaryReader.GetSettledContributionsAsync(
            [pledge.PersonId],
            [pledge.AccountId.Value],
            pledge.StartDate,
            pledge.EndDate,
            cancellationToken
        );
        _ = settledMap.TryGetValue((pledge.PersonId, pledge.AccountId.Value), out var fulfilled);

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        return FinancialPledgeMapper.ToResponse(pledge, accountName, fulfilled, today);
    }
}
