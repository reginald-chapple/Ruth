using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.Modules.Events.Domain;
using Ruth.Modules.Events.Domain.InteractiveSessions;
using Ruth.Modules.Events.Infrastructure;
using Ruth.Modules.Events.Infrastructure.Realtime;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.InteractiveSessions;

internal sealed class ModerateInteractiveResponseCommandHandler(
    EventsDbContext dbContext,
    IInteractiveSessionBroadcaster broadcaster
) : ICommandHandler<ModerateInteractiveResponseCommand, InteractiveStageProjectionResponse>
{
    public async Task<Result<InteractiveStageProjectionResponse>> HandleAsync(
        ModerateInteractiveResponseCommand command,
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

        var moderated = session.ModerateResponse(
            InteractiveResponseId.From(command.ResponseId),
            command.ModerationStatus,
            command.IncrementUpvote
        );
        if (moderated.IsFailure)
        {
            return Result.Failure<InteractiveStageProjectionResponse>(moderated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        var projection = InteractiveStageProjectionCalculator.BuildStageProjection(session);
        await broadcaster.BroadcastStageProjectionAsync(projection, cancellationToken);
        return projection;
    }
}
