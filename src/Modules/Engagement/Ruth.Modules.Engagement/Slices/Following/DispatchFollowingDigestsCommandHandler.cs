using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Events;
using Ruth.Modules.Engagement.Contracts.Following;
using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.Modules.Engagement.Domain.Following;
using Ruth.Modules.Engagement.Domain.Reminders;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Following;

internal sealed class DispatchFollowingDigestsCommandHandler(
    EngagementDbContext dbContext,
    IClock clock
) : ICommandHandler<DispatchFollowingDigestsCommand, FollowingDigestRunResponse>
{
    public async Task<Result<FollowingDigestRunResponse>> HandleAsync(
        DispatchFollowingDigestsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var pending = await dbContext
            .FollowingEventNotifications.Where(static e => !e.DigestProcessedAtUtc.HasValue)
            .OrderBy(static e => e.OccurredAtUtc)
            .ToListAsync(cancellationToken);
        if (pending.Count == 0)
        {
            return new FollowingDigestRunResponse(0, 0);
        }

        var activeSubs = await dbContext
            .FollowingSubscriptions.Where(static s => s.IsSubscribed)
            .ToListAsync(cancellationToken);

        var now = clock.UtcNow;
        var byFollower = GroupSummariesByFollower(pending, activeSubs, now);
        foreach (var (followerId, items) in byFollower)
        {
            EnqueueFollowerDigest(followerId, items, now);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return new FollowingDigestRunResponse(byFollower.Count, pending.Count);
    }

    private static Dictionary<Guid, List<string>> GroupSummariesByFollower(
        List<FollowingEventNotification> pending,
        List<FollowingSubscription> activeSubs,
        DateTimeOffset now
    )
    {
        var byFollower = new Dictionary<Guid, List<string>>();
        foreach (var ev in pending)
        {
            var matching = activeSubs.Where(s =>
                string.Equals(s.EntityTypeKey, ev.EntityTypeKey, StringComparison.OrdinalIgnoreCase)
                && s.EntityId == ev.EntityId
            );
            foreach (var sub in matching)
            {
                if (!byFollower.TryGetValue(sub.FollowerPersonId, out var summaries))
                {
                    summaries = [];
                    byFollower[sub.FollowerPersonId] = summaries;
                }

                summaries.Add(ev.Summary);
                sub.StampNotified(now);
            }

            ev.MarkProcessed(now);
        }

        return byFollower;
    }

    private void EnqueueFollowerDigest(Guid followerId, List<string> items, DateTimeOffset now)
    {
        var digestText = string.Join(" | ", items.Distinct(StringComparer.Ordinal));
        _ = dbContext.NotificationMessages.Add(
            NotificationMessage.Create(
                followerId,
                $"Following Digest ({items.Count} update(s))",
                digestText,
                NotificationCategory.FollowingDigest,
                "/engagement/notifications"
            )
        );
        dbContext.EnqueueIntegrationEvent(
            new SystemNotificationRequestedIntegrationEvent(
                Guid.CreateVersion7(),
                now,
                "engagement.following-digest",
                RecipientPersonId: followerId,
                MergeFields: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["DigestSummary"] = digestText,
                },
                TriggerModule: "Engagement.Following"
            )
        );
    }
}
