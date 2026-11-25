using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Verification;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Domain.Verification;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Verification;

internal sealed class RecordPersonViewedCommandHandler(CrmDbContext dbContext, IClock clock)
    : ICommandHandler<RecordPersonViewedCommand, PersonViewedResponse>
{
    public async Task<Result<PersonViewedResponse>> HandleAsync(
        RecordPersonViewedCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var targetId = PersonId.From(command.TargetPersonId);
        var person = await dbContext.People.FirstOrDefaultAsync(
            p => p.Id == targetId,
            cancellationToken
        );
        if (person is null)
        {
            return CrmErrors.PersonNotFound;
        }

        var entry = PersonViewed.Create(
            targetId,
            clock.UtcNow,
            command.ViewerPersonId.HasValue ? PersonId.From(command.ViewerPersonId.Value) : null,
            command.ViewerUserId,
            command.IpAddress,
            command.Source
        );
        person.IncrementViewedCount();

        _ = dbContext.PersonViewedEntries.Add(entry);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            new PersonViewedResponse(
                entry.Id.Value,
                entry.TargetPersonId.Value,
                entry.ViewerPersonId?.Value,
                entry.ViewerUserId,
                entry.ViewedOnUtc,
                entry.IpAddress,
                entry.Source
            )
        );
    }
}
