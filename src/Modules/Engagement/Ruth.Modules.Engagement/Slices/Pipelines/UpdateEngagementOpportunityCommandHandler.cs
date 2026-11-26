using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class UpdateEngagementOpportunityCommandHandler(EngagementDbContext dbContext)
    : ICommandHandler<UpdateEngagementOpportunityCommand, EngagementOpportunityResponse>
{
    public async Task<Result<EngagementOpportunityResponse>> HandleAsync(
        UpdateEngagementOpportunityCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = EngagementOpportunityId.From(command.OpportunityId);
        var opp = await dbContext.EngagementOpportunities.FirstOrDefaultAsync(
            o => o.Id == id,
            cancellationToken
        );
        if (opp is null)
        {
            return EngagementErrors.OpportunityNotFound;
        }

        var pipeline = await dbContext
            .PipelineTypes.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == PipelineTypeId.From(command.Spec.PipelineTypeId),
                cancellationToken
            );
        if (pipeline is null)
        {
            return EngagementErrors.PipelineTypeNotFound;
        }

        var slug = command.Spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        var slugConflict = await dbContext.EngagementOpportunities.AnyAsync(
            o => o.Slug == slug && o.Id != id,
            cancellationToken
        );
        if (slugConflict)
        {
            return EngagementErrors.OpportunitySlugConflict;
        }

        var res = opp.Update(command.Spec);
        if (res.IsFailure)
        {
            return Result.Failure<EngagementOpportunityResponse>(res.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PipelineProjectionMapper.ToResponse(opp, pipeline.Name);
    }
}
