using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Reminders;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Reminders;

internal sealed class CreateReminderCommandHandler(EngagementDbContext dbContext)
    : ICommandHandler<CreateReminderCommand, ReminderResponse>
{
    public async Task<Result<ReminderResponse>> HandleAsync(
        CreateReminderCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var type = await dbContext
            .ReminderTypes.AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == ReminderTypeId.From(command.ReminderTypeId) && t.IsActive,
                cancellationToken
            );
        if (type is null)
        {
            return EngagementErrors.ReminderTypeNotFound;
        }

        var created = Reminder.Create(
            type,
            command.AssigneePersonId,
            command.EntityTypeKey,
            command.EntityId,
            command.EntitySummary,
            command.Note,
            command.ReminderDate,
            command.RenewPeriodDays,
            command.RenewMaxCount
        );
        if (created.IsFailure)
        {
            return Result.Failure<ReminderResponse>(created.Errors);
        }

        _ = dbContext.Reminders.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return ReminderProjectionMapper.ToResponse(created.Value, type);
    }
}
