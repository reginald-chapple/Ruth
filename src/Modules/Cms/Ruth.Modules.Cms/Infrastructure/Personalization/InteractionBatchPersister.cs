using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Personalization;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Infrastructure.Personalization;

internal sealed class InteractionBatchPersister(CmsDbContext db, IClock clock)
{
    public async Task PersistBatchAsync(
        IReadOnlyList<TrackInteractionItemSpec> batch,
        CancellationToken cancellationToken
    )
    {
        var nowUtc = clock.UtcNow;
        var channelsByKey = await LoadOrCreateChannelsAsync(batch, cancellationToken);
        var componentsByKey = await LoadOrCreateComponentsAsync(
            batch,
            channelsByKey,
            cancellationToken
        );
        var sessionsByKey = await LoadOrCreateSessionsAsync(batch, nowUtc, cancellationToken);

        foreach (var item in batch)
        {
            StageInteraction(item, componentsByKey, sessionsByKey, nowUtc);
        }

        _ = await db.SaveChangesAsync(cancellationToken);
    }

    private void StageInteraction(
        TrackInteractionItemSpec item,
        Dictionary<(string ChannelKey, string ComponentKey), InteractionComponent> componentsByKey,
        Dictionary<string, InteractionSession> sessionsByKey,
        DateTimeOffset nowUtc
    )
    {
        var chKey = CmsTextNormalizer.NormalizeKey(item.ChannelKey);
        var compKey = CmsTextNormalizer.NormalizeKey(item.ComponentKey);
        if (!componentsByKey.TryGetValue((chKey, compKey), out var component))
        {
            return;
        }

        InteractionSessionId? sessionId = null;
        if (
            !string.IsNullOrWhiteSpace(item.SessionKey)
            && sessionsByKey.TryGetValue(item.SessionKey.Trim(), out var session)
        )
        {
            session.Touch(item.OccurredDateTimeUtc ?? nowUtc, item.PersonId);
            sessionId = session.Id;
        }

        var interactionRes = Interaction.Create(
            component.Id,
            string.IsNullOrWhiteSpace(item.Operation) ? "View" : item.Operation,
            item.OccurredDateTimeUtc ?? nowUtc,
            sessionId,
            item.PersonId,
            item.Summary,
            item.SourceUrl,
            item.ChannelCustom1,
            item.ChannelCustom2,
            item.InteractionData
        );
        if (interactionRes.IsSuccess)
        {
            _ = db.Interactions.Add(interactionRes.Value);
        }
    }

    private async Task<Dictionary<string, InteractionChannel>> LoadOrCreateChannelsAsync(
        IReadOnlyList<TrackInteractionItemSpec> batch,
        CancellationToken ct
    )
    {
        List<string> keys =
        [
            .. batch
                .Select(static i => CmsTextNormalizer.NormalizeKey(i.ChannelKey))
                .Where(static k => k.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase),
        ];

        var existing = await db
            .InteractionChannels.Where(c => keys.Contains(c.Key))
            .ToDictionaryAsync(static c => c.Key, StringComparer.OrdinalIgnoreCase, ct);

        foreach (var key in keys)
        {
            if (existing.ContainsKey(key))
            {
                continue;
            }

            var res = InteractionChannel.Create(key, key, InteractionChannelType.Website);
            if (res.IsSuccess)
            {
                _ = db.InteractionChannels.Add(res.Value);
                existing[key] = res.Value;
            }
        }

        return existing;
    }

    private async Task<
        Dictionary<(string ChannelKey, string ComponentKey), InteractionComponent>
    > LoadOrCreateComponentsAsync(
        IReadOnlyList<TrackInteractionItemSpec> batch,
        Dictionary<string, InteractionChannel> channelsByKey,
        CancellationToken ct
    )
    {
        List<InteractionChannelId> channelIds = [.. channelsByKey.Values.Select(static c => c.Id)];
        var existingList = await db
            .InteractionComponents.Where(c => channelIds.Contains(c.InteractionChannelId))
            .ToListAsync(ct);

        var channelKeyById = channelsByKey.Values.ToDictionary(static c => c.Id, static c => c.Key);
        var map = new Dictionary<(string, string), InteractionComponent>();
        foreach (var comp in existingList)
        {
            if (channelKeyById.TryGetValue(comp.InteractionChannelId, out var chKey))
            {
                map[(chKey, comp.ComponentKey)] = comp;
            }
        }

        foreach (var item in batch)
        {
            var chKey = CmsTextNormalizer.NormalizeKey(item.ChannelKey);
            var compKey = CmsTextNormalizer.NormalizeKey(item.ComponentKey);
            if (
                chKey.Length == 0
                || compKey.Length == 0
                || map.ContainsKey((chKey, compKey))
                || !channelsByKey.TryGetValue(chKey, out var channel)
            )
            {
                continue;
            }

            var compName = string.IsNullOrWhiteSpace(item.ComponentName)
                ? compKey
                : item.ComponentName.Trim();
            var res = InteractionComponent.Create(channel.Id, compKey, compName);
            if (res.IsSuccess)
            {
                _ = db.InteractionComponents.Add(res.Value);
                map[(chKey, compKey)] = res.Value;
            }
        }

        return map;
    }

    private async Task<Dictionary<string, InteractionSession>> LoadOrCreateSessionsAsync(
        IReadOnlyList<TrackInteractionItemSpec> batch,
        DateTimeOffset nowUtc,
        CancellationToken ct
    )
    {
        List<string> sessionKeys =
        [
            .. batch
                .Select(static i => i.SessionKey?.Trim())
                .Where(static k => !string.IsNullOrWhiteSpace(k))
                .OfType<string>()
                .Distinct(StringComparer.Ordinal),
        ];

        if (sessionKeys.Count == 0)
        {
            return new Dictionary<string, InteractionSession>(StringComparer.Ordinal);
        }

        var existing = await db
            .InteractionSessions.Where(s => sessionKeys.Contains(s.SessionKey))
            .ToDictionaryAsync(static s => s.SessionKey, StringComparer.Ordinal, ct);

        foreach (var item in batch)
        {
            var key = item.SessionKey?.Trim();
            if (string.IsNullOrWhiteSpace(key) || existing.ContainsKey(key))
            {
                continue;
            }

            var res = InteractionSession.Create(
                key,
                item.OccurredDateTimeUtc ?? nowUtc,
                item.PersonId,
                item.BranchId,
                item.DeviceType,
                item.Browser,
                item.OperatingSystem,
                item.IpAddressHash
            );
            if (res.IsSuccess)
            {
                _ = db.InteractionSessions.Add(res.Value);
                existing[key] = res.Value;
            }
        }

        return existing;
    }
}
