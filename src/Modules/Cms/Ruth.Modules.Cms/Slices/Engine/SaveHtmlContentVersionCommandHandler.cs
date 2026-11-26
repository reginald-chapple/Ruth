using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class SaveHtmlContentVersionCommandHandler(CmsDbContext db, IClock clock)
    : ICommandHandler<SaveHtmlContentVersionCommand, HtmlContentResponse>
{
    public async Task<Result<HtmlContentResponse>> HandleAsync(
        SaveHtmlContentVersionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var blockId = CmsBlockId.From(command.BlockId);
        var blockExists = await db
            .Blocks.AsNoTracking()
            .AnyAsync(b => b.Id == blockId, cancellationToken);
        if (!blockExists)
        {
            return Result.Failure<HtmlContentResponse>(
                CmsErrors.NotFound("Block", command.BlockId.ToString("D"))
            );
        }

        var maxVersion = await db
            .HtmlContents.AsNoTracking()
            .Where(h => h.BlockId == blockId && h.EntityValue == command.EntityValue)
            .MaxAsync(static h => (int?)h.Version, cancellationToken)
            ?? 0;

        var now = clock.UtcNow;
        var res = HtmlContent.Create(
            blockId,
            maxVersion + 1,
            command.Content,
            command.EntityValue,
            command.ContentSummary,
            command.StartDateTimeUtc,
            command.ExpireDateTimeUtc,
            command.AutoApprove,
            command.ApprovedByPersonId,
            command.AutoApprove ? now : null
        );
        if (res.IsFailure)
        {
            return Result.Failure<HtmlContentResponse>(res.PrimaryError);
        }

        _ = db.HtmlContents.Add(res.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsProjectionMapper.ToResponse(res.Value));
    }
}
