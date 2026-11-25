using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Scheduling;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class RemovePersonScheduleExclusionCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<RemovePersonScheduleExclusionCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        RemovePersonScheduleExclusionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = PersonScheduleExclusionId.From(command.ExclusionId);
        var existing = await dbContext.PersonScheduleExclusions.FirstOrDefaultAsync(
            e => e.Id == id,
            cancellationToken
        );
        if (existing is null)
        {
            return GroupsErrors.ExclusionNotFound;
        }

        _ = dbContext.PersonScheduleExclusions.Remove(existing);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
