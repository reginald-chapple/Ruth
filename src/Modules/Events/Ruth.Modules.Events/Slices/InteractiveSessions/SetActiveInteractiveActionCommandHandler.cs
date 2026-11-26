using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.Modules.Events.Domain;
using Ruth.Modules.Events.Domain.InteractiveSessions;
using Ruth.Modules.Events.Infrastructure;
using Ruth.Modules.Events.Infrastructure.Realtime;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.InteractiveSessions;

internal sealed class SetActiveInteractiveActionCommandHandler(
    EventsDbContext dbContext,
    IInteractiveSessionBroadcaster broadcaster
) : ICommandHandler<SetActiveInteractiveActionCommand, InteractiveStageProjectionResponse>
{
    public async Task<Result<InteractiveStageProjectionResponse>> HandleAsync(
        SetActiveInteractiveActionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var session = await InteractiveSessionResolver.LoadTrackedByIdAsync(
            dbContext,
            command.SessionId,
            cancellationToken
        );
        if (session is null)
        {
            return EventsErrors.InteractiveSessionNotFound;
        }

        var actionId = command.ActionId.HasValue
            ? InteractiveActionId.From(command.ActionId.Value)
            : (InteractiveActionId?)null;
        var updated = session.SetActiveAction(actionId);
        if (updated.IsFailure)
        {
            return Result.Failure<InteractiveStageProjectionResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        var projection = InteractiveStageProjectionCalculator.BuildStageProjection(session);
        await broadcaster.BroadcastStageProjectionAsync(projection, cancellationToken);
        return projection;
    }
}
