using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Signals;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Signals;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Signals;

internal sealed class RemovePersonSignalCommandHandler(CrmDbContext dbContext, IClock clock)
    : ICommandHandler<RemovePersonSignalCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        RemovePersonSignalCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = PersonSignalId.From(command.PersonSignalId);
        var signal = await dbContext.PersonSignals.FirstOrDefaultAsync(
            s => s.Id == id,
            cancellationToken
        );
        if (signal is null)
        {
            return CrmErrors.PersonSignalNotFound;
        }

        var personId = signal.PersonId;
        _ = dbContext.PersonSignals.Remove(signal);

        var person = await dbContext.People.FirstOrDefaultAsync(
            p => p.Id == personId,
            cancellationToken
        );
        if (person is not null)
        {
            var remaining = await dbContext
                .PersonSignals.Where(s => s.PersonId == personId && s.Id != id)
                .ToListAsync(cancellationToken);
            var types = await dbContext
                .SignalTypes.AsNoTracking()
                .ToDictionaryAsync(static t => t.Id, cancellationToken);
            TopSignalCalculator.Recalculate(person, remaining, types, clock.UtcNow);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
