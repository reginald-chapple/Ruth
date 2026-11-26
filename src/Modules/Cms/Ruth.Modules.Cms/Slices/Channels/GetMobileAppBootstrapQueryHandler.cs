using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Channels;

internal sealed class GetMobileAppBootstrapQueryHandler(CmsDbContext db, IClock clock)
    : IQueryHandler<GetMobileAppBootstrapQuery, MobileAppBootstrapResponse>
{
    public async Task<Result<MobileAppBootstrapResponse>> HandleAsync(
        GetMobileAppBootstrapQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var slug = CmsTextNormalizer.NormalizeSlug(query.AppSlug);
        var app = await db
            .MobileApplications.AsNoTracking()
            .Include(static a => a.Layouts)
            .Include(static a => a.Pages)
            .Include(static a => a.DeepLinks)
            .FirstOrDefaultAsync(a => a.Slug == slug && a.IsActive, cancellationToken);
        return app is null
            ? Result.Failure<MobileAppBootstrapResponse>(
                CmsErrors.NotFound("MobileApplication", query.AppSlug)
            )
            : Result.Success(
                new MobileAppBootstrapResponse(
                    app.ApiVersion,
                    app.Id.Value,
                    app.Slug,
                    app.Name,
                    app.BundleIdentifier,
                    app.MinimumClientVersion,
                    app.LatestClientVersion,
                    app.IsClientUpgradeRequired(query.ClientVersion),
                    app.NavigationType,
                    app.PrimaryColorHex,
                    app.SecondaryColorHex,
                    [
                        .. app
                            .Layouts.OrderBy(static l => l.Order)
                            .Select(CmsChannelProjectionMapper.ToResponse),
                    ],
                    [
                        .. app
                            .Pages.Where(static p => p.IsActive && p.ShowInNavigation)
                            .OrderBy(static p => p.Order)
                            .Select(CmsChannelProjectionMapper.ToResponse),
                    ],
                    [
                        .. app
                            .DeepLinks.Where(static d => d.IsActive)
                            .Select(CmsChannelProjectionMapper.ToResponse),
                    ],
                    clock.UtcNow
                )
            );
    }
}
