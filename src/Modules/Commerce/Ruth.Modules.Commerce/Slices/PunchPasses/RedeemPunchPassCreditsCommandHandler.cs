using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Events;
using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.PunchPasses;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Commerce.Slices.Orders;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Commerce.Slices.PunchPasses;

internal sealed class RedeemPunchPassCreditsCommandHandler(
    CommerceDbContext dbContext,
    MemberEligibilityResolver eligibilityResolver,
    WalletPassCommerceSyncCollaborator walletPassSync,
    IClock clock
) : ICommandHandler<RedeemPunchPassCreditsCommand, IssuedPunchPassResponse>
{
    public async Task<Result<IssuedPunchPassResponse>> HandleAsync(
        RedeemPunchPassCreditsCommand command,
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

        var beneficiary = command.RedeemedForPersonId ?? pass.OwnerPersonId;
        var sameHousehold = await eligibilityResolver.AreInSameHouseholdAsync(
            pass.OwnerPersonId,
            beneficiary,
            cancellationToken
        );

        var nowUtc = clock.UtcNow;
        var redeemed = pass.Redeem(
            command.CreditsToRedeem,
            beneficiary,
            sameHousehold,
            command.BranchId,
            command.ReferenceNotes,
            nowUtc
        );
        if (redeemed.IsFailure)
        {
            return Result.Failure<IssuedPunchPassResponse>(redeemed.Errors);
        }

        await SaveAndSyncRedemptionAsync(pass, command.CreditsToRedeem, nowUtc, cancellationToken);
        var typeName = await LoadPassTypeNameAsync(
            dbContext,
            pass.PunchPassTypeId,
            cancellationToken
        );
        return CommerceOrderMapper.MapIssuedPunchPass(pass, typeName);
    }

    internal static async Task<string> LoadPassTypeNameAsync(
        CommerceDbContext dbContext,
        PunchPassTypeId typeId,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .PunchPassTypes.AsNoTracking()
            .Where(t => t.Id == typeId)
            .Select(static t => t.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Punch Pass";
    }

    private async Task SaveAndSyncRedemptionAsync(
        IssuedPunchPass pass,
        int creditsRedeemed,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        var totalCredits = await walletPassSync.ComputeActivePunchCreditsAsync(
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
                -creditsRedeemed,
                pass.RemainingCredits,
                totalCredits
            )
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await walletPassSync.SyncPunchPassBalanceAsync(
            pass.OwnerPersonId,
            totalCredits,
            cancellationToken
        );
    }
}
