using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Pipelines;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class CreateEngagementRequestCommandHandler(
    EngagementDbContext dbContext,
    IQueryDispatcher queryDispatcher,
    IClock clock
) : ICommandHandler<CreateEngagementRequestCommand, EngagementRequestResponse>
{
    public async Task<Result<EngagementRequestResponse>> HandleAsync(
        CreateEngagementRequestCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var opp = await dbContext
            .EngagementOpportunities.AsNoTracking()
            .FirstOrDefaultAsync(
                o => o.Id == EngagementOpportunityId.From(command.EngagementOpportunityId),
                cancellationToken
            );
        if (opp is null)
        {
            return EngagementErrors.OpportunityNotFound;
        }

        var pipeline = await dbContext
            .PipelineTypes.AsNoTracking()
            .Include(static p => p.Stages)
            .FirstOrDefaultAsync(
                p => p.Id == PipelineTypeId.From(opp.PipelineTypeId),
                cancellationToken
            );
        return pipeline is null
            ? EngagementErrors.PipelineTypeNotFound
            : await CreateAndSaveAsync(command, pipeline, opp, cancellationToken);
    }

    private async Task<Result<EngagementRequestResponse>> CreateAndSaveAsync(
        CreateEngagementRequestCommand command,
        PipelineType pipeline,
        EngagementOpportunity opp,
        CancellationToken cancellationToken
    )
    {
        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var spec = new EngagementRequestCreationSpec(
            command.RequesterPersonId,
            command.BranchId,
            command.AssignedConnectorPersonId,
            command.PlacementGroupId,
            command.PlacementGroupRoleId,
            command.FollowUpDate,
            command.Comments
        );
        var created = EngagementRequest.Create(pipeline, opp, spec, today, now);
        if (created.IsFailure)
        {
            return Result.Failure<EngagementRequestResponse>(created.Errors);
        }

        _ = dbContext.EngagementRequests.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var people = await PipelineProjectionMapper.LoadPeopleAsync(
            queryDispatcher,
            [command.RequesterPersonId],
            cancellationToken
        );
        return PipelineProjectionMapper.ToResponse(
            created.Value,
            pipeline,
            opp.Name,
            people,
            today,
            now
        );
    }
}
