using Ruth.Modules.Engagement.Contracts.Following;
using Ruth.Modules.Engagement.Domain.Following;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Following;

internal sealed class RecordFollowedEntityEventCommandHandler(
    EngagementDbContext dbContext,
    IClock clock
) : ICommandHandler<RecordFollowedEntityEventCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        RecordFollowedEntityEventCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var created = FollowingEventNotification.Create(
            command.EntityTypeKey,
            command.EntityId,
            command.EventTypeKey,
            command.Summary,
            clock.UtcNow
        );
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Errors);
        }

        _ = dbContext.FollowingEventNotifications.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return created.Value.Id.Value;
    }
}
