using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class CreateCmsBlockCommandHandler(CmsDbContext db)
    : ICommandHandler<CreateCmsBlockCommand, CmsBlockResponse>
{
    public async Task<Result<CmsBlockResponse>> HandleAsync(
        CreateCmsBlockCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedKey = CmsTextNormalizer.NormalizeKey(command.BlockTypeKey);
        var blockType = await db
            .BlockTypes.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Key == normalizedKey, cancellationToken);
        if (blockType is null)
        {
            return Result.Failure<CmsBlockResponse>(
                CmsErrors.NotFound("BlockType", command.BlockTypeKey)
            );
        }

        var mergedSettings = new Dictionary<string, string?>(
            blockType.DefaultSettings,
            StringComparer.OrdinalIgnoreCase
        );
        if (command.Settings is not null)
        {
            foreach (var (k, v) in command.Settings)
            {
                mergedSettings[k] = v;
            }
        }

        var blockRes = CmsBlock.Create(
            blockType.Id,
            command.Zone,
            command.Name,
            command.PageId is { } pid ? CmsPageId.From(pid) : null,
            command.LayoutId is { } lid ? CmsLayoutId.From(lid) : null,
            command.SiteId is { } sid ? CmsSiteId.From(sid) : null,
            command.Order,
            command.PreHtml,
            command.PostHtml,
            command.CssClass,
            command.OutputCacheDurationSeconds,
            mergedSettings
        );
        if (blockRes.IsFailure)
        {
            return Result.Failure<CmsBlockResponse>(blockRes.PrimaryError);
        }

        _ = db.Blocks.Add(blockRes.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsProjectionMapper.ToResponse(blockRes.Value, blockType));
    }
}
