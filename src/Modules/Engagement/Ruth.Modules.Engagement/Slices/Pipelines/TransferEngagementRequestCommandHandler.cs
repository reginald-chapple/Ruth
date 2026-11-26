using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class TransferEngagementRequestCommandHandler(
    EngagementDbContext dbContext,
    IQueryDispatcher queryDispatcher,
    IClock clock
) : ICommandHandler<TransferEngagementRequestCommand, EngagementRequestResponse>
{
    public async Task<Result<EngagementRequestResponse>> HandleAsync(
        TransferEngagementRequestCommand command,
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

        var targetOpp = await dbContext
            .EngagementOpportunities.AsNoTracking()
            .FirstOrDefaultAsync(
                o => o.Id == EngagementOpportunityId.From(command.TargetOpportunityId),
                cancellationToken
            );
        if (targetOpp is null)
        {
            return EngagementErrors.OpportunityNotFound;
        }

        var targetPipe = await dbContext
            .PipelineTypes.AsNoTracking()
            .Include(static p => p.Stages)
            .FirstOrDefaultAsync(
                p => p.Id == PipelineTypeId.From(targetOpp.PipelineTypeId),
                cancellationToken
            );
        if (targetPipe is null)
        {
            return EngagementErrors.PipelineTypeNotFound;
        }

        var now = clock.UtcNow;
        request.TransferTo(targetPipe, targetOpp, command.Note, command.ActorPersonId, now);
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
