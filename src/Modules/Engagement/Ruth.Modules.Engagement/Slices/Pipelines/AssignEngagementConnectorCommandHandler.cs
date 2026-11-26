using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class AssignEngagementConnectorCommandHandler(
    EngagementDbContext dbContext,
    IQueryDispatcher queryDispatcher,
    IClock clock
) : ICommandHandler<AssignEngagementConnectorCommand, EngagementRequestResponse>
{
    public async Task<Result<EngagementRequestResponse>> HandleAsync(
        AssignEngagementConnectorCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var request = await dbContext
            .EngagementRequests.Include(static r => r.Activities)
            .FirstOrDefaultAsync(
                r => r.Id == EngagementRequestId.From(command.RequestId),
                cancellationToken
            );
        if (request is null)
        {
            return EngagementErrors.EngagementRequestNotFound;
        }

        var now = clock.UtcNow;
        request.AssignConnector(command.ConnectorPersonId, command.Note, now);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return await PipelineRequestResponseLoader.BuildResponseAsync(
            dbContext,
            queryDispatcher,
            request,
            now,
            cancellationToken
        );
    }
}
