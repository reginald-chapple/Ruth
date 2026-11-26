using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Personalization;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.Modules.Cms.Infrastructure.Personalization;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Personalization;

internal sealed class ResolveAdaptiveMessageQueryHandler(
    CmsDbContext db,
    AdaptiveMessageContextLoader contextLoader,
    IInteractionBuffer interactionBuffer,
    IClock clock
) : IQueryHandler<ResolveAdaptiveMessageQuery, ResolvedAdaptiveMessageResponse>
{
    public async Task<Result<ResolvedAdaptiveMessageResponse>> HandleAsync(
        ResolveAdaptiveMessageQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var key = CmsTextNormalizer.NormalizeKey(query.AdaptiveMessageKey);
        var message = await db
            .AdaptiveMessages.AsNoTracking()
            .Include(static m => m.Adaptations)
            .FirstOrDefaultAsync(m => m.Key == key, cancellationToken);
        if (message is null)
        {
            return Result.Failure<ResolvedAdaptiveMessageResponse>(
                CmsErrors.NotFound("AdaptiveMessage", query.AdaptiveMessageKey)
            );
        }

        var now = clock.UtcNow;
        var resolved = await EvaluateAsync(query, message, now, cancellationToken);
        return Result.Success(resolved);
    }

    private async Task<ResolvedAdaptiveMessageResponse> EvaluateAsync(
        ResolveAdaptiveMessageQuery query,
        AdaptiveMessage message,
        DateTimeOffset now,
        CancellationToken ct
    )
    {
        var matchedSegments = await contextLoader.ResolveMatchedSegmentsAsync(
            query.PersonId,
            query.RequestContext,
            now,
            ct
        );
        var recentViews = await contextLoader.LoadRecentViewsAsync(
            message,
            query.PersonId,
            now,
            ct
        );

        var (selected, reason) = AdaptiveMessageResolver.SelectAdaptation(
            message,
            matchedSegments,
            recentViews,
            now
        );

        await RecordViewIfRequestedAsync(query, message, selected, now, ct);

        return new ResolvedAdaptiveMessageResponse(
            message.Id.Value,
            message.Key,
            selected is null ? null : CmsChannelProjectionMapper.ToResponse(selected),
            [.. matchedSegments.Order(StringComparer.OrdinalIgnoreCase)],
            reason
        );
    }

    private async Task RecordViewIfRequestedAsync(
        ResolveAdaptiveMessageQuery query,
        AdaptiveMessage message,
        AdaptiveMessageAdaptation? selected,
        DateTimeOffset now,
        CancellationToken ct
    )
    {
        if (selected is null || !query.RecordViewInteraction)
        {
            return;
        }

        await interactionBuffer.EnqueueAsync(
            new TrackInteractionItemSpec(
                "adaptive-messages",
                $"adaptation-{selected.Id.Value:N}",
                selected.Name,
                Operation: "View",
                PersonId: query.PersonId,
                Summary: $"Viewed adaptive message '{message.Key}' adaptation '{selected.Name}'.",
                OccurredDateTimeUtc: now
            ),
            ct
        );
    }
}
