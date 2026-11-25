using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.Scheduling;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class AddPersonScheduleExclusionCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<AddPersonScheduleExclusionCommand, PersonScheduleExclusionResponse>
{
    public async Task<Result<PersonScheduleExclusionResponse>> HandleAsync(
        AddPersonScheduleExclusionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var groupId = command.GroupId.HasValue
            ? GroupId.From(command.GroupId.Value)
            : (GroupId?)null;
        var parentId = command.ParentExclusionId.HasValue
            ? PersonScheduleExclusionId.From(command.ParentExclusionId.Value)
            : (PersonScheduleExclusionId?)null;

        var created = PersonScheduleExclusion.Create(
            command.PersonId,
            command.StartDate,
            command.EndDate,
            command.Title,
            groupId,
            parentId
        );
        if (created.IsFailure)
        {
            return Result.Failure<PersonScheduleExclusionResponse>(created.Errors);
        }

        _ = dbContext.PersonScheduleExclusions.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResponse(created.Value));
    }

    internal static PersonScheduleExclusionResponse ToResponse(PersonScheduleExclusion exclusion)
    {
        return new PersonScheduleExclusionResponse(
            exclusion.Id.Value,
            exclusion.PersonId,
            exclusion.GroupId?.Value,
            exclusion.ParentExclusionId?.Value,
            exclusion.Title,
            exclusion.StartDate,
            exclusion.EndDate
        );
    }
}
