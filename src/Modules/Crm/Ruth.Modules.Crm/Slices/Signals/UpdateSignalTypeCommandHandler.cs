using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Signals;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Signals;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Signals;

internal sealed class UpdateSignalTypeCommandHandler(CrmDbContext dbContext, IClock clock)
    : ICommandHandler<UpdateSignalTypeCommand, SignalTypeResponse>
{
    public async Task<Result<SignalTypeResponse>> HandleAsync(
        UpdateSignalTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = SignalTypeId.From(command.SignalTypeId);
        var signalType = await dbContext.SignalTypes.FirstOrDefaultAsync(
            s => s.Id == id,
            cancellationToken
        );
        if (signalType is null)
        {
            return CrmErrors.SignalTypeNotFound;
        }

        var updated = signalType.Update(
            command.Name,
            command.SignalColor,
            command.SignalIcon,
            command.Description,
            command.Order
        );
        if (updated.IsFailure)
        {
            return Result.Failure<SignalTypeResponse>(updated.Errors);
        }

        var duplicateName = await dbContext.SignalTypes.AnyAsync(
            s => s.Id != id && s.NormalizedName == signalType.NormalizedName,
            cancellationToken
        );
        if (duplicateName)
        {
            return CrmErrors.SignalTypeDuplicate;
        }

        await RecalculateAffectedPeopleAsync(id, clock.UtcNow, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(CreateSignalTypeCommandHandler.ToResponse(signalType));
    }

    private async Task RecalculateAffectedPeopleAsync(
        SignalTypeId signalTypeId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var affectedPersonIds = await dbContext
            .PersonSignals.Where(s => s.SignalTypeId == signalTypeId)
            .Select(static s => s.PersonId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        if (affectedPersonIds.Length == 0)
        {
            return;
        }

        var people = await dbContext
            .People.Where(p => affectedPersonIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        var allSignals = await dbContext
            .PersonSignals.Where(s => affectedPersonIds.Contains(s.PersonId))
            .ToListAsync(cancellationToken);
        var types = await dbContext.SignalTypes.ToDictionaryAsync(
            static t => t.Id,
            cancellationToken
        );

        foreach (var person in people)
        {
            TopSignalCalculator.Recalculate(
                person,
                allSignals.Where(s => s.PersonId == person.Id),
                types,
                nowUtc
            );
        }
    }
}
