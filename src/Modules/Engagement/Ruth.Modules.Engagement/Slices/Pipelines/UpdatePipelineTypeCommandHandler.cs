using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class UpdatePipelineTypeCommandHandler(EngagementDbContext dbContext)
    : ICommandHandler<UpdatePipelineTypeCommand, PipelineTypeResponse>
{
    public async Task<Result<PipelineTypeResponse>> HandleAsync(
        UpdatePipelineTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = PipelineTypeId.From(command.PipelineTypeId);
        var pipeline = await dbContext
            .PipelineTypes.Include(static p => p.Stages)
            .Include(static p => p.ActivityTypes)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pipeline is null)
        {
            return EngagementErrors.PipelineTypeNotFound;
        }

        var normalizedSlug = command.Spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        var slugConflict = await dbContext.PipelineTypes.AnyAsync(
            p => p.Slug == normalizedSlug && p.Id != id,
            cancellationToken
        );
        if (slugConflict)
        {
            return EngagementErrors.PipelineSlugConflict;
        }

        var res = pipeline.Update(command.Spec);
        if (res.IsFailure)
        {
            return Result.Failure<PipelineTypeResponse>(res.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PipelineProjectionMapper.ToResponse(pipeline);
    }
}
