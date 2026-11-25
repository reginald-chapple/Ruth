using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Scheduling;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class AddGroupScheduleExclusionCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<AddGroupScheduleExclusionCommand, GroupScheduleExclusionResponse>
{
    public async Task<Result<GroupScheduleExclusionResponse>> HandleAsync(
        AddGroupScheduleExclusionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var typeId = GroupTypeId.From(command.GroupTypeId);
        var exists = await dbContext.GroupTypes.AnyAsync(gt => gt.Id == typeId, cancellationToken);
        if (!exists)
        {
            return GroupsErrors.GroupTypeNotFound;
        }

        var created = GroupScheduleExclusion.Create(
            typeId,
            command.StartDate,
            command.EndDate,
            command.Title
        );
        if (created.IsFailure)
        {
            return Result.Failure<GroupScheduleExclusionResponse>(created.Errors);
        }

        _ = dbContext.GroupScheduleExclusions.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResponse(created.Value));
    }

    internal static GroupScheduleExclusionResponse ToResponse(GroupScheduleExclusion exclusion)
    {
        return new GroupScheduleExclusionResponse(
            exclusion.Id.Value,
            exclusion.GroupTypeId.Value,
            exclusion.Title,
            exclusion.StartDate,
            exclusion.EndDate
        );
    }
}
