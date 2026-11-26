using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.Modules.Events.Domain;
using Ruth.Modules.Events.Domain.InteractiveSessions;
using Ruth.Modules.Events.Infrastructure;
using Ruth.Modules.Events.Infrastructure.Realtime;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.InteractiveSessions;

internal sealed class EndInteractiveSessionCommandHandler(
    EventsDbContext dbContext,
    TimeProvider timeProvider,
    IInteractiveSessionBroadcaster broadcaster
) : ICommandHandler<EndInteractiveSessionCommand, InteractiveSessionDetailResponse>
{
    public async Task<Result<InteractiveSessionDetailResponse>> HandleAsync(
        EndInteractiveSessionCommand command,
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

        _ = session.EndSession(timeProvider.GetUtcNow());
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var projection = InteractiveStageProjectionCalculator.BuildStageProjection(session);
        await broadcaster.BroadcastStageProjectionAsync(projection, cancellationToken);
        return InteractiveSessionMapper.MapDetail(session);
    }
}
