using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.Modules.Engagement.Domain.Reminders;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Reminders;

internal sealed class CreateReminderTypeCommandHandler(EngagementDbContext dbContext)
    : ICommandHandler<CreateReminderTypeCommand, ReminderTypeResponse>
{
    public async Task<Result<ReminderTypeResponse>> HandleAsync(
        CreateReminderTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var created = ReminderType.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<ReminderTypeResponse>(created.Errors);
        }

        _ = dbContext.ReminderTypes.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return ReminderProjectionMapper.ToResponse(created.Value);
    }
}
