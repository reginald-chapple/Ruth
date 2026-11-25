using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Scheduling;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class CreateScheduleTemplateCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<CreateScheduleTemplateCommand, GroupMemberScheduleTemplateResponse>
{
    public async Task<Result<GroupMemberScheduleTemplateResponse>> HandleAsync(
        CreateScheduleTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var typeId = command.GroupTypeId.HasValue
            ? GroupTypeId.From(command.GroupTypeId.Value)
            : (GroupTypeId?)null;
        var created = GroupMemberScheduleTemplate.Create(
            command.Name,
            command.ScheduleId,
            typeId,
            command.RecurrenceDescription,
            command.IntervalWeeks
        );
        if (created.IsFailure)
        {
            return Result.Failure<GroupMemberScheduleTemplateResponse>(created.Errors);
        }

        _ = dbContext.ScheduleTemplates.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResponse(created.Value));
    }

    internal static GroupMemberScheduleTemplateResponse ToResponse(
        GroupMemberScheduleTemplate template
    )
    {
        return new GroupMemberScheduleTemplateResponse(
            template.Id.Value,
            template.Name,
            template.GroupTypeId?.Value,
            template.ScheduleId,
            template.RecurrenceDescription,
            template.IntervalWeeks
        );
    }
}
