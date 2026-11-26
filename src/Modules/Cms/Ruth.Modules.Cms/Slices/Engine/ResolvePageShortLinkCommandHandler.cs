using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class ResolvePageShortLinkCommandHandler(CmsDbContext db, IClock clock)
    : ICommandHandler<ResolvePageShortLinkCommand, PageShortLinkResponse>
{
    public async Task<Result<PageShortLinkResponse>> HandleAsync(
        ResolvePageShortLinkCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var token = CmsTextNormalizer.NormalizeSlug(command.Token);
        var link = await db.PageShortLinks.FirstOrDefaultAsync(
            l => l.Token == token,
            cancellationToken
        );
        if (link is null)
        {
            return Result.Failure<PageShortLinkResponse>(
                CmsErrors.NotFound("ShortLink", command.Token)
            );
        }

        link.RecordClick(clock.UtcNow);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsProjectionMapper.ToResponse(link));
    }
}
