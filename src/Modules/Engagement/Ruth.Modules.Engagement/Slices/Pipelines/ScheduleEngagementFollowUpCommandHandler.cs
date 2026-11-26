using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class ScheduleEngagementFollowUpCommandHandler(
    EngagementDbContext dbContext,
    IQueryDispatcher queryDispatcher,
    IClock clock
) : ICommandHandler<ScheduleEngagementFollowUpCommand, EngagementRequestResponse>
{
    public async Task<Result<EngagementRequestResponse>> HandleAsync(
        ScheduleEngagementFollowUpCommand command,
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

        var pipeline = await dbContext
            .PipelineTypes.AsNoTracking()
            .Include(static p => p.Stages)
            .FirstOrDefaultAsync(
                p => p.Id == PipelineTypeId.From(request.PipelineTypeId),
                cancellationToken
            );
        if (pipeline is null)
        {
            return EngagementErrors.PipelineTypeNotFound;
        }

        var now = clock.UtcNow;
        var res = request.ScheduleFollowUp(
            pipeline,
            command.FollowUpDate,
            command.ActorPersonId,
            command.Note,
            DateOnly.FromDateTime(now.UtcDateTime),
            now
        );
        if (res.IsFailure)
        {
            return Result.Failure<EngagementRequestResponse>(res.Errors);
        }

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
