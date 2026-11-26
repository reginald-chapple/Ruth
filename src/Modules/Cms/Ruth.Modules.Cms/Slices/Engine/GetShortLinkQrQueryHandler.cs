using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class GetShortLinkQrQueryHandler(CmsDbContext db)
    : IQueryHandler<GetShortLinkQrQuery, ShortLinkQrResponse>
{
    public async Task<Result<ShortLinkQrResponse>> HandleAsync(
        GetShortLinkQrQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var token = CmsTextNormalizer.NormalizeSlug(query.Token);
        var link = await db
            .PageShortLinks.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Token == token, cancellationToken);
        if (link is null)
        {
            return Result.Failure<ShortLinkQrResponse>(
                CmsErrors.NotFound("ShortLink", query.Token)
            );
        }

        var origin = string.IsNullOrWhiteSpace(query.BaseOrigin)
            ? "https://ruth.local"
            : query.BaseOrigin.Trim().TrimEnd('/');
        var shortUrl = $"{origin}/go/{link.Token}";
        var svg = QrCodeSvgGenerator.GenerateSvg(shortUrl);

        return Result.Success(
            new ShortLinkQrResponse(
                link.Id.Value,
                link.Token,
                shortUrl,
                link.BuildResolvedTargetUrl(),
                svg
            )
        );
    }
}
