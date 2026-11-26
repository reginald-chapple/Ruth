using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Channels;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Channels;

internal sealed class GetHydratedMobilePageQueryHandler(
    CmsDbContext db,
    IQueryDispatcher queries,
    IClock clock
) : IQueryHandler<GetHydratedMobilePageQuery, HydratedMobilePageResponse>
{
    public async Task<Result<HydratedMobilePageResponse>> HandleAsync(
        GetHydratedMobilePageQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var slug = CmsTextNormalizer.NormalizeSlug(query.AppSlug);
        var pageKey = CmsTextNormalizer.NormalizeKey(query.PageKey);

        var app = await db
            .MobileApplications.AsNoTracking()
            .Include(static a => a.Layouts)
            .Include(static a => a.Pages)
            .FirstOrDefaultAsync(a => a.Slug == slug && a.IsActive, cancellationToken);
        if (app is null)
        {
            return Result.Failure<HydratedMobilePageResponse>(
                CmsErrors.NotFound("MobileApplication", query.AppSlug)
            );
        }

        var page = app.Pages.FirstOrDefault(p =>
            p.IsActive && string.Equals(p.PageKey, pageKey, StringComparison.OrdinalIgnoreCase)
        );
        return page is null
            ? Result.Failure<HydratedMobilePageResponse>(
                CmsErrors.NotFound("MobilePage", query.PageKey)
            )
            : Result.Success(await HydratePageAsync(app, page, query, cancellationToken));
    }

    private async Task<HydratedMobilePageResponse> HydratePageAsync(
        MobileApplication app,
        MobilePage page,
        GetHydratedMobilePageQuery query,
        CancellationToken ct
    )
    {
        var layout = page.MobileLayoutId is { } lid
            ? app.Layouts.FirstOrDefault(l => l.Id == lid)
            : null;
        var channelItems = await LoadChannelItemsAsync(page.ContentChannelId, ct);
        var adaptive = await ResolveAdaptiveAsync(page.AdaptiveMessageKey, query, ct);

        return new HydratedMobilePageResponse(
            app.ApiVersion,
            CmsChannelProjectionMapper.ToResponse(page),
            layout is null ? null : CmsChannelProjectionMapper.ToResponse(layout),
            channelItems,
            adaptive
        );
    }

    private async Task<IReadOnlyList<ContentChannelItemResponse>> LoadChannelItemsAsync(
        ContentChannelId? channelId,
        CancellationToken ct
    )
    {
        if (channelId is not { } cid)
        {
            return [];
        }

        var now = clock.UtcNow;
        var items = await db
            .ContentChannelItems.AsNoTracking()
            .Include(static i => i.ChildAssociations)
            .Where(i => i.ContentChannelId == cid)
            .OrderByDescending(static i => i.StartDateTimeUtc)
            .Take(25)
            .ToListAsync(ct);

        return
        [
            .. items.Where(i => i.IsPublishedAt(now)).Select(CmsContentProjectionMapper.ToResponse),
        ];
    }

    private async Task<ResolvedAdaptiveMessageResponse?> ResolveAdaptiveAsync(
        string? messageKey,
        GetHydratedMobilePageQuery query,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(messageKey))
        {
            return null;
        }

        var res = await queries.QueryAsync<
            ResolveAdaptiveMessageQuery,
            ResolvedAdaptiveMessageResponse
        >(new ResolveAdaptiveMessageQuery(messageKey, query.PersonId, query.RequestContext), ct);
        return res.IsSuccess ? res.Value : null;
    }
}
