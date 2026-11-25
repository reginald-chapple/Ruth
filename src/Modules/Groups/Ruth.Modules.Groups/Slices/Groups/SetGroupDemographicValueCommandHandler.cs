using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Groups;

internal sealed class SetGroupDemographicValueCommandHandler(
    GroupsDbContext dbContext,
    IClock clock
) : ICommandHandler<SetGroupDemographicValueCommand, GroupDemographicValueResponse>
{
    public async Task<Result<GroupDemographicValueResponse>> HandleAsync(
        SetGroupDemographicValueCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var groupId = GroupId.From(command.GroupId);
        var typeId = GroupDemographicTypeId.From(command.GroupDemographicTypeId);

        if (!await dbContext.Groups.AnyAsync(g => g.Id == groupId, cancellationToken))
        {
            return GroupsErrors.GroupNotFound;
        }

        var demographicType = await dbContext
            .GroupDemographicTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == typeId, cancellationToken);
        if (demographicType is null)
        {
            return GroupsErrors.DemographicTypeNotFound;
        }

        var upsert = await UpsertValueAsync(groupId, typeId, command, cancellationToken);
        if (upsert.IsFailure)
        {
            return Result.Failure<GroupDemographicValueResponse>(upsert.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(
            GroupMapper.ToDemographicValueResponse(upsert.Value, demographicType.Name)
        );
    }

    private async Task<Result<GroupDemographicValue>> UpsertValueAsync(
        GroupId groupId,
        GroupDemographicTypeId typeId,
        SetGroupDemographicValueCommand command,
        CancellationToken cancellationToken
    )
    {
        var nowUtc = clock.UtcNow;
        var existing = await dbContext.GroupDemographicValues.FirstOrDefaultAsync(
            v => v.GroupId == groupId && v.GroupDemographicTypeId == typeId,
            cancellationToken
        );
        if (existing is not null)
        {
            var updated = existing.Update(
                command.Value,
                command.RelatedEntityTypeKey,
                command.RelatedEntityId,
                nowUtc
            );
            return updated.IsFailure
                ? Result.Failure<GroupDemographicValue>(updated.Errors)
                : Result.Success(existing);
        }

        var created = GroupDemographicValue.Create(
            groupId,
            typeId,
            command.Value,
            command.RelatedEntityTypeKey,
            command.RelatedEntityId,
            nowUtc
        );
        if (created.IsSuccess)
        {
            _ = dbContext.GroupDemographicValues.Add(created.Value);
        }

        return created;
    }
}
