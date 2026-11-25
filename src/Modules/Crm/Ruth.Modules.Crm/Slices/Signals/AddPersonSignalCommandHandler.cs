using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Signals;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Domain.Signals;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Signals;

internal sealed class AddPersonSignalCommandHandler(CrmDbContext dbContext, IClock clock)
    : ICommandHandler<AddPersonSignalCommand, PersonSignalResponse>
{
    public async Task<Result<PersonSignalResponse>> HandleAsync(
        AddPersonSignalCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var personId = PersonId.From(command.PersonId);
        var signalTypeId = SignalTypeId.From(command.SignalTypeId);
        var ownerPersonId = PersonId.From(command.OwnerPersonId);

        var person = await dbContext.People.FirstOrDefaultAsync(
            p => p.Id == personId,
            cancellationToken
        );
        if (person is null)
        {
            return CrmErrors.PersonNotFound;
        }

        var signalType = await dbContext
            .SignalTypes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == signalTypeId, cancellationToken);
        if (signalType is null)
        {
            return CrmErrors.SignalTypeNotFound;
        }

        var created = PersonSignal.Create(
            personId,
            signalTypeId,
            ownerPersonId,
            command.Note,
            command.ExpirationDateUtc
        );
        if (created.IsFailure)
        {
            return Result.Failure<PersonSignalResponse>(created.Errors);
        }

        var nowUtc = clock.UtcNow;
        var signal = created.Value;
        await PersistAndRecalculateTopSignalAsync(person, signal, nowUtc, cancellationToken);

        return Result.Success(
            new PersonSignalResponse(
                signal.Id.Value,
                signal.PersonId.Value,
                signalType.Id.Value,
                signalType.Name,
                signalType.SignalColor,
                signalType.SignalIcon,
                signalType.Order,
                signal.OwnerPersonId.Value,
                signal.Note,
                signal.ExpirationDateUtc,
                signal.IsActiveAt(nowUtc),
                signal.CreatedOnUtc
            )
        );
    }

    private async Task PersistAndRecalculateTopSignalAsync(
        Person person,
        PersonSignal signal,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        _ = dbContext.PersonSignals.Add(signal);
        var existingSignals = await dbContext
            .PersonSignals.Where(s => s.PersonId == person.Id)
            .ToListAsync(cancellationToken);
        existingSignals.Add(signal);

        var allTypes = await dbContext
            .SignalTypes.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id, cancellationToken);
        TopSignalCalculator.Recalculate(person, existingSignals, allTypes, nowUtc);

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
