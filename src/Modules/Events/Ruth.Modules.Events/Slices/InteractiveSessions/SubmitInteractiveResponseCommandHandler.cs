using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.Modules.Events.Domain;
using Ruth.Modules.Events.Domain.InteractiveSessions;
using Ruth.Modules.Events.Infrastructure;
using Ruth.Modules.Events.Infrastructure.Realtime;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.InteractiveSessions;

internal sealed class SubmitInteractiveResponseCommandHandler(
    EventsDbContext dbContext,
    TimeProvider timeProvider,
    IInteractiveSessionBroadcaster broadcaster
) : ICommandHandler<SubmitInteractiveResponseCommand, InteractiveStageProjectionResponse>
{
    public async Task<Result<InteractiveStageProjectionResponse>> HandleAsync(
        SubmitInteractiveResponseCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var session = await InteractiveSessionResolver.LoadByIdOrJoinCodeAsync(
            dbContext,
            command.SessionIdOrJoinCode,
            asNoTracking: false,
            cancellationToken
        );
        if (session is null)
        {
            return EventsErrors.InteractiveSessionNotFound;
        }

        var submitted = session.SubmitResponse(
            InteractiveActionId.From(command.ActionId),
            command.ParticipantToken,
            command.SelectedOption,
            command.TextResponse,
            command.ResponderPersonId,
            timeProvider.GetUtcNow()
        );
        if (submitted.IsFailure)
        {
            return Result.Failure<InteractiveStageProjectionResponse>(submitted.Errors);
        }

        _ = dbContext.InteractiveResponses.Add(submitted.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var projection = InteractiveStageProjectionCalculator.BuildStageProjection(session);
        await broadcaster.BroadcastStageProjectionAsync(projection, cancellationToken);
        return projection;
    }
}
