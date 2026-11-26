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

internal sealed class IssuePunchPassCommandHandler(
    CommerceDbContext dbContext,
    PunchPassCreditCalculator creditCalculator,
    IClock clock
) : ICommandHandler<IssuePunchPassCommand, IssuedPunchPassResponse>
{
    public async Task<Result<IssuedPunchPassResponse>> HandleAsync(
        IssuePunchPassCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var typeId = PunchPassTypeId.From(command.PunchPassTypeId);
        var passType = await dbContext.PunchPassTypes.FirstOrDefaultAsync(
            t => t.Id == typeId,
            cancellationToken
        );
        if (passType is null)
        {
            return CommerceErrors.PunchPassTypeNotFound;
        }

        var nowUtc = clock.UtcNow;
        var issued = IssuedPunchPass.Issue(
            passType,
            command.OwnerPersonId,
            packMultiplier: 1,
            sourceOrderId: null,
            command.ReferenceNotes,
            nowUtc
        );
        if (issued.IsFailure)
        {
            return Result.Failure<IssuedPunchPassResponse>(issued.Errors);
        }

        _ = dbContext.IssuedPunchPasses.Add(issued.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var totalCredits = await creditCalculator.ComputeActivePunchCreditsAsync(
            command.OwnerPersonId,
            nowUtc,
            cancellationToken
        );
        dbContext.EnqueueIntegrationEvent(
            new PunchPassBalanceChangedIntegrationEvent(
                Guid.CreateVersion7(),
                nowUtc,
                issued.Value.Id.Value,
                issued.Value.PassCode,
                command.OwnerPersonId,
                issued.Value.InitialCredits,
                issued.Value.RemainingCredits,
                totalCredits
            )
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return CommerceOrderMapper.MapIssuedPunchPass(issued.Value, passType.Name);
    }
}
