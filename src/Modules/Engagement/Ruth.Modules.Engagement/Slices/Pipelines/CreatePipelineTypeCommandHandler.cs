using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Pipelines;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class CreatePipelineTypeCommandHandler(EngagementDbContext dbContext)
    : ICommandHandler<CreatePipelineTypeCommand, PipelineTypeResponse>
{
    public async Task<Result<PipelineTypeResponse>> HandleAsync(
        CreatePipelineTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var created = PipelineType.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<PipelineTypeResponse>(created.Errors);
        }

        var slugExists = await dbContext.PipelineTypes.AnyAsync(
            p => p.Slug == created.Value.Slug,
            cancellationToken
        );
        if (slugExists)
        {
            return EngagementErrors.PipelineSlugConflict;
        }

        _ = dbContext.PipelineTypes.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PipelineProjectionMapper.ToResponse(created.Value);
    }
}
