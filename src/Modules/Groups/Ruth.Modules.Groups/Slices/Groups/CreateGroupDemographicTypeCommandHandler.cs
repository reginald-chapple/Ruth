using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Groups;

internal sealed class CreateGroupDemographicTypeCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<CreateGroupDemographicTypeCommand, GroupDemographicTypeResponse>
{
    public async Task<Result<GroupDemographicTypeResponse>> HandleAsync(
        CreateGroupDemographicTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var groupTypeId = GroupTypeId.From(command.GroupTypeId);
        var exists = await dbContext.GroupTypes.AnyAsync(
            gt => gt.Id == groupTypeId,
            cancellationToken
        );
        if (!exists)
        {
            return GroupsErrors.GroupTypeNotFound;
        }

        var created = GroupDemographicType.Create(
            groupTypeId,
            command.Name,
            command.ComponentKey,
            command.Description,
            command.IsAutomated,
            command.RunOnPersonUpdate
        );
        if (created.IsFailure)
        {
            return Result.Failure<GroupDemographicTypeResponse>(created.Errors);
        }

        _ = dbContext.GroupDemographicTypes.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(GroupMapper.ToDemographicTypeResponse(created.Value));
    }
}
