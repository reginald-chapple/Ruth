using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class RecordEngagementActivityCommandHandler(
    EngagementDbContext dbContext,
    IQueryDispatcher queryDispatcher,
    IClock clock
) : ICommandHandler<RecordEngagementActivityCommand, EngagementRequestResponse>
{
    public async Task<Result<EngagementRequestResponse>> HandleAsync(
        RecordEngagementActivityCommand command,
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

        var activityName = command.ActivityName;
        if (string.IsNullOrWhiteSpace(activityName) && command.ActivityTypeId.HasValue)
        {
            activityName = await dbContext
                .PipelineActivityTypes.AsNoTracking()
                .Where(a => a.Id == PipelineActivityTypeId.From(command.ActivityTypeId.Value))
                .Select(static a => a.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var now = clock.UtcNow;
        var res = request.RecordActivity(
            command.ActivityTypeId,
            activityName ?? string.Empty,
            command.ConnectorPersonId,
            command.Note,
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
