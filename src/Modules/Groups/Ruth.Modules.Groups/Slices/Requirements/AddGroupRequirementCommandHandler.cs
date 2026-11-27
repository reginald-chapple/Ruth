using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Requirements;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Requirements;

internal sealed class AddGroupRequirementCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<AddGroupRequirementCommand, GroupRequirementResponse>
{
    public async Task<Result<GroupRequirementResponse>> HandleAsync(
        AddGroupRequirementCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var reqTypeId = GroupRequirementTypeId.From(command.GroupRequirementTypeId);
        var reqType = await dbContext
            .GroupRequirementTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == reqTypeId, cancellationToken);
        if (reqType is null)
        {
            return GroupsErrors.RequirementTypeNotFound;
        }

        var groupId = command.GroupId.HasValue
            ? GroupId.From(command.GroupId.Value)
            : (GroupId?)null;
        var groupTypeId = command.GroupTypeId.HasValue
            ? GroupTypeId.From(command.GroupTypeId.Value)
            : (GroupTypeId?)null;
        var roleId = command.GroupRoleId.HasValue
            ? GroupTypeRoleId.From(command.GroupRoleId.Value)
            : (GroupTypeRoleId?)null;

        var created = GroupRequirement.Create(
            reqTypeId,
            groupId,
            groupTypeId,
            roleId,
            command.MustMeetRequirementToAddMember,
            command.AllowLeadersToOverride,
            command.AppliesToAgeClassification,
            command.DueDateStaticDate,
            command.DueDateAttributeKey
        );
        if (created.IsFailure)
        {
            return Result.Failure<GroupRequirementResponse>(created.Errors);
        }

        _ = dbContext.GroupRequirements.Add(created.Value);
        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            dbContext.ChangeTracker.Clear();
            return DomainError.Conflict(
                "Groups.RequirementAlreadyExists",
                "This requirement is already attached to the specified group, group type, and role scope."
            );
        }

        return Result.Success(ToResponse(created.Value, reqType));
    }

    internal static GroupRequirementResponse ToResponse(
        GroupRequirement requirement,
        GroupRequirementType type
    )
    {
        return new GroupRequirementResponse(
            requirement.Id.Value,
            requirement.GroupId?.Value,
            requirement.GroupTypeId?.Value,
            requirement.GroupRequirementTypeId.Value,
            type.Name,
            type.CheckType,
            requirement.GroupRoleId?.Value,
            requirement.MustMeetRequirementToAddMember,
            requirement.AllowLeadersToOverride,
            requirement.AppliesToAgeClassification,
            requirement.DueDateStaticDate,
            requirement.DueDateAttributeKey
        );
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException pg
            && string.Equals(
                pg.SqlState,
                PostgresErrorCodes.UniqueViolation,
                StringComparison.Ordinal
            );
    }
}
