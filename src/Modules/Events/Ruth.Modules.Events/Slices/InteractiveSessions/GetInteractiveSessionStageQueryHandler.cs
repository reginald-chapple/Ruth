using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.Modules.Events.Domain;
using Ruth.Modules.Events.Domain.InteractiveSessions;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.InteractiveSessions;

internal sealed class GetInteractiveSessionStageQueryHandler(EventsDbContext dbContext)
    : IQueryHandler<GetInteractiveSessionStageQuery, InteractiveStageProjectionResponse>
{
    public async Task<Result<InteractiveStageProjectionResponse>> HandleAsync(
        GetInteractiveSessionStageQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var session = await InteractiveSessionResolver.LoadByIdOrJoinCodeAsync(
            dbContext,
            query.SessionIdOrJoinCode,
            asNoTracking: true,
            cancellationToken
        );
        return session is null
            ? EventsErrors.InteractiveSessionNotFound
            : InteractiveStageProjectionCalculator.BuildStageProjection(session);
    }
}
