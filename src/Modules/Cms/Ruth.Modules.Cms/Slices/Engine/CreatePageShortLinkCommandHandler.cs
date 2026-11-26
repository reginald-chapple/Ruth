using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class CreatePageShortLinkCommandHandler(CmsDbContext db)
    : ICommandHandler<CreatePageShortLinkCommand, PageShortLinkResponse>
{
    public async Task<Result<PageShortLinkResponse>> HandleAsync(
        CreatePageShortLinkCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var siteId = CmsSiteId.From(command.SiteId);
        var siteExists = await db
            .Sites.AsNoTracking()
            .AnyAsync(s => s.Id == siteId, cancellationToken);
        if (!siteExists)
        {
            return Result.Failure<PageShortLinkResponse>(
                CmsErrors.NotFound("Site", command.SiteId.ToString("D"))
            );
        }

        var linkRes = PageShortLink.Create(
            siteId,
            command.Token,
            command.TargetUrl,
            command.CategoryId,
            command.IsPinned,
            command.UtmSource,
            command.UtmMedium,
            command.UtmCampaign
        );
        if (linkRes.IsFailure)
        {
            return Result.Failure<PageShortLinkResponse>(linkRes.PrimaryError);
        }

        var exists = await db
            .PageShortLinks.AsNoTracking()
            .AnyAsync(l => l.Token == linkRes.Value.Token, cancellationToken);
        if (exists)
        {
            return Result.Failure<PageShortLinkResponse>(
                CmsErrors.Conflict(
                    "ShortLink.DuplicateToken",
                    $"Shortlink token '{linkRes.Value.Token}' already exists."
                )
            );
        }

        _ = db.PageShortLinks.Add(linkRes.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsProjectionMapper.ToResponse(linkRes.Value));
    }
}
