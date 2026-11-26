using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Events;
using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Commerce.Slices.Orders;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Commerce.Slices.PunchPasses;

internal sealed class AdjustPunchPassBalanceCommandHandler(
    CommerceDbContext dbContext,
    PunchPassCreditCalculator creditCalculator,
    IClock clock
) : ICommandHandler<AdjustPunchPassBalanceCommand, IssuedPunchPassResponse>
{
    public async Task<Result<IssuedPunchPassResponse>> HandleAsync(
        AdjustPunchPassBalanceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var passId = IssuedPunchPassId.From(command.IssuedPunchPassId);
        var pass = await dbContext
            .IssuedPunchPasses.Include(static p => p.LedgerEntries)
            .FirstOrDefaultAsync(p => p.Id == passId, cancellationToken);
        if (pass is null)
        {
            return CommerceErrors.PunchPassNotFound;
        }

        var nowUtc = clock.UtcNow;
        var adjusted = pass.AdjustBalance(command.DeltaCredits, command.Reason, nowUtc);
        if (adjusted.IsFailure)
        {
            return Result.Failure<IssuedPunchPassResponse>(adjusted.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var totalCredits = await creditCalculator.ComputeActivePunchCreditsAsync(
            pass.OwnerPersonId,
            nowUtc,
            cancellationToken
        );
        dbContext.EnqueueIntegrationEvent(
            new PunchPassBalanceChangedIntegrationEvent(
                Guid.CreateVersion7(),
                nowUtc,
                pass.Id.Value,
                pass.PassCode,
                pass.OwnerPersonId,
                command.DeltaCredits,
                pass.RemainingCredits,
                totalCredits
            )
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var typeName = await RedeemPunchPassCreditsCommandHandler.LoadPassTypeNameAsync(
            dbContext,
            pass.PunchPassTypeId,
            cancellationToken
        );
        return CommerceOrderMapper.MapIssuedPunchPass(pass, typeName);
    }
}
