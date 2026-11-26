using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.Modules.Events.Domain;
using Ruth.Modules.Events.Domain.InteractiveSessions;
using Ruth.Modules.Events.Infrastructure;
using Ruth.Modules.Events.Infrastructure.Realtime;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.InteractiveSessions;

internal sealed class StartInteractiveSessionOccurrenceCommandHandler(
    EventsDbContext dbContext,
    TimeProvider timeProvider,
    IInteractiveSessionBroadcaster broadcaster
) : ICommandHandler<StartInteractiveSessionOccurrenceCommand, InteractiveSessionDetailResponse>
{
    public async Task<Result<InteractiveSessionDetailResponse>> HandleAsync(
        StartInteractiveSessionOccurrenceCommand command,
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

        var started = session.StartOccurrence(
            command.EventItemOccurrenceId,
            command.BranchId,
            timeProvider.GetUtcNow()
        );
        if (started.IsFailure)
        {
            return Result.Failure<InteractiveSessionDetailResponse>(started.Errors);
        }

        _ = dbContext.InteractiveSessionOccurrences.Add(started.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var projection = InteractiveStageProjectionCalculator.BuildStageProjection(session);
        await broadcaster.BroadcastStageProjectionAsync(projection, cancellationToken);
        return InteractiveSessionMapper.MapDetail(session);
    }
}
