using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Personalization;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Personalization;

internal sealed class GetInteractionsQueryHandler(CmsDbContext db)
    : IQueryHandler<GetInteractionsQuery, IReadOnlyList<InteractionResponse>>
{
    public async Task<Result<IReadOnlyList<InteractionResponse>>> HandleAsync(
        GetInteractionsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var channels = await db
            .InteractionChannels.AsNoTracking()
            .ToDictionaryAsync(static c => c.Id, cancellationToken);
        var components = await db
            .InteractionComponents.AsNoTracking()
            .ToDictionaryAsync(static c => c.Id, cancellationToken);

        var q = BuildFilteredQuery(query, channels, components, out var emptyChannel);
        if (emptyChannel)
        {
            return Result.Success<IReadOnlyList<InteractionResponse>>([]);
        }

        var take = Math.Clamp(query.MaxCount, 1, 200);
        var rows = await q.OrderByDescending(static i => i.OccurredDateTimeUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<InteractionResponse>>(
            ProjectRows(rows, channels, components)
        );
    }

    private IQueryable<Interaction> BuildFilteredQuery(
        GetInteractionsQuery query,
        Dictionary<InteractionChannelId, InteractionChannel> channels,
        Dictionary<InteractionComponentId, InteractionComponent> components,
        out bool emptyChannel
    )
    {
        emptyChannel = false;
        var q = db.Interactions.AsNoTracking();
        if (query.PersonId is { } pid)
        {
            q = q.Where(i => i.PersonId == pid);
        }

        if (string.IsNullOrWhiteSpace(query.ChannelKey))
        {
            return q;
        }

        var key = CmsTextNormalizer.NormalizeKey(query.ChannelKey);
        var ch = channels.Values.FirstOrDefault(c =>
            string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase)
        );
        if (ch is null)
        {
            emptyChannel = true;
            return q;
        }

        List<InteractionComponentId> compIds =
        [
            .. components
                .Values.Where(c => c.InteractionChannelId == ch.Id)
                .Select(static c => c.Id),
        ];
        return q.Where(i => compIds.Contains(i.InteractionComponentId));
    }

    private static List<InteractionResponse> ProjectRows(
        List<Interaction> rows,
        Dictionary<InteractionChannelId, InteractionChannel> channels,
        Dictionary<InteractionComponentId, InteractionComponent> components
    )
    {
        List<InteractionResponse> results = [];
        foreach (var row in rows)
        {
            if (
                components.TryGetValue(row.InteractionComponentId, out var comp)
                && channels.TryGetValue(comp.InteractionChannelId, out var ch)
            )
            {
                results.Add(
                    new InteractionResponse(
                        row.Id.Value,
                        ch.Id.Value,
                        ch.Key,
                        comp.Id.Value,
                        comp.ComponentKey,
                        row.InteractionSessionId?.Value,
                        row.PersonId,
                        row.Operation,
                        row.Summary,
                        row.SourceUrl,
                        row.OccurredDateTimeUtc,
                        row.InteractionData
                    )
                );
            }
        }

        return results;
    }
}
