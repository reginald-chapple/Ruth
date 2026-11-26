using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class UpdateCmsBlockCommandHandler(CmsDbContext db)
    : ICommandHandler<UpdateCmsBlockCommand, CmsBlockResponse>
{
    public async Task<Result<CmsBlockResponse>> HandleAsync(
        UpdateCmsBlockCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var blockId = CmsBlockId.From(command.BlockId);
        var block = await db.Blocks.FirstOrDefaultAsync(b => b.Id == blockId, cancellationToken);
        if (block is null)
        {
            return Result.Failure<CmsBlockResponse>(
                CmsErrors.NotFound("Block", command.BlockId.ToString("D"))
            );
        }

        var updateRes = block.UpdateConfiguration(
            command.Zone,
            command.Name,
            command.Order,
            command.IsActive,
            command.PreHtml,
            command.PostHtml,
            command.CssClass,
            command.Settings
        );
        if (updateRes.IsFailure)
        {
            return Result.Failure<CmsBlockResponse>(updateRes.PrimaryError);
        }

        _ = await db.SaveChangesAsync(cancellationToken);
        var blockType = await db
            .BlockTypes.AsNoTracking()
            .FirstAsync(bt => bt.Id == block.BlockTypeId, cancellationToken);
        return Result.Success(CmsProjectionMapper.ToResponse(block, blockType));
    }
}
