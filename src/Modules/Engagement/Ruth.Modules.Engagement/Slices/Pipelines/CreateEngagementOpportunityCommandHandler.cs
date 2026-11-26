using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Pipelines;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class CreateEngagementOpportunityCommandHandler(EngagementDbContext dbContext)
    : ICommandHandler<CreateEngagementOpportunityCommand, EngagementOpportunityResponse>
{
    public async Task<Result<EngagementOpportunityResponse>> HandleAsync(
        CreateEngagementOpportunityCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
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

        var created = EngagementOpportunity.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<EngagementOpportunityResponse>(created.Errors);
        }

        var slugExists = await dbContext.EngagementOpportunities.AnyAsync(
            o => o.Slug == created.Value.Slug,
            cancellationToken
        );
        if (slugExists)
        {
            return EngagementErrors.OpportunitySlugConflict;
        }

        _ = dbContext.EngagementOpportunities.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PipelineProjectionMapper.ToResponse(created.Value, pipeline.Name);
    }
}
