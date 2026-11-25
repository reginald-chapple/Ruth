using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Domain.Requirements;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Requirements;

internal sealed class CreateGroupRequirementTypeCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<CreateGroupRequirementTypeCommand, GroupRequirementTypeResponse>
{
    public async Task<Result<GroupRequirementTypeResponse>> HandleAsync(
        CreateGroupRequirementTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = GroupRequirementType.Create(
            command.Name,
            command.CheckType,
            command.Description,
            command.Summary,
            command.RuleKey,
            command.RuleValue,
            command.CanExpire,
            command.ExpireInDays,
            command.WarningDaysBeforeExpiration,
            command.PositiveLabel,
            command.NegativeLabel,
            command.WarningLabel,
            command.CheckboxLabel,
            command.IconCssClass,
            command.DueDateType,
            command.DueDateOffsetInDays,
            command.CategoryId
        );
        if (created.IsFailure)
        {
            return Result.Failure<GroupRequirementTypeResponse>(created.Errors);
        }

        _ = dbContext.GroupRequirementTypes.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResponse(created.Value));
    }

    internal static GroupRequirementTypeResponse ToResponse(GroupRequirementType type)
    {
        return new GroupRequirementTypeResponse(
            type.Id.Value,
            type.Name,
            type.Description,
            type.Summary,
            type.CheckType,
            type.RuleKey,
            type.RuleValue,
            type.CanExpire,
            type.ExpireInDays,
            type.WarningDaysBeforeExpiration,
            type.PositiveLabel,
            type.NegativeLabel,
            type.WarningLabel,
            type.CheckboxLabel,
            type.IconCssClass,
            type.DueDateType,
            type.DueDateOffsetInDays,
            type.CategoryId
        );
    }
}
