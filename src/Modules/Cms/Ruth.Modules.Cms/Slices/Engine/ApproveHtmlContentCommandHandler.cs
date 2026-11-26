using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class ApproveHtmlContentCommandHandler(CmsDbContext db, IClock clock)
    : ICommandHandler<ApproveHtmlContentCommand, HtmlContentResponse>
{
    public async Task<Result<HtmlContentResponse>> HandleAsync(
        ApproveHtmlContentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = HtmlContentId.From(command.HtmlContentId);
        var html = await db.HtmlContents.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        if (html is null)
        {
            return Result.Failure<HtmlContentResponse>(
                CmsErrors.NotFound("HtmlContent", command.HtmlContentId.ToString("D"))
            );
        }

        html.SetApproval(command.IsApproved, command.ApprovedByPersonId, clock.UtcNow);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsProjectionMapper.ToResponse(html));
    }
}
