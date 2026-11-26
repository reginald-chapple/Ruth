using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Channels;

internal sealed class ResolveMobileDeepLinkQueryHandler(CmsDbContext db)
    : IQueryHandler<ResolveMobileDeepLinkQuery, ResolvedMobileDeepLinkResponse>
{
    public async Task<Result<ResolvedMobileDeepLinkResponse>> HandleAsync(
        ResolveMobileDeepLinkQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var slug = CmsTextNormalizer.NormalizeSlug(query.AppSlug);
        var app = await db
            .MobileApplications.AsNoTracking()
            .Include(static a => a.Pages)
            .Include(static a => a.DeepLinks)
            .FirstOrDefaultAsync(a => a.Slug == slug && a.IsActive, cancellationToken);
        return app is null
            ? Result.Failure<ResolvedMobileDeepLinkResponse>(
                CmsErrors.NotFound("MobileApplication", query.AppSlug)
            )
            : Result.Success(app.ResolveDeepLink(query.Path));
    }
}
