using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.Modules.Events.Domain;
using Ruth.Modules.Events.Domain.InteractiveSessions;
using Ruth.Modules.Events.Infrastructure;
using Ruth.Modules.Events.Infrastructure.Realtime;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.InteractiveSessions;

internal sealed class AddInteractiveActionCommandHandler(
    EventsDbContext dbContext,
    IInteractiveSessionBroadcaster broadcaster
) : ICommandHandler<AddInteractiveActionCommand, InteractiveActionResponse>
{
    public async Task<Result<InteractiveActionResponse>> HandleAsync(
        AddInteractiveActionCommand command,
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

        var added = session.AddAction(command.Spec);
        if (added.IsFailure)
        {
            return Result.Failure<InteractiveActionResponse>(added.Errors);
        }

        _ = dbContext.InteractiveActions.Add(added.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        if (session.Status == InteractiveSessionStatus.Live)
        {
            var projection = InteractiveStageProjectionCalculator.BuildStageProjection(session);
            await broadcaster.BroadcastStageProjectionAsync(projection, cancellationToken);
        }

        return InteractiveStageProjectionCalculator.MapAction(added.Value);
    }
}
