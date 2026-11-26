using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Achievements;
using Ruth.Modules.Engagement.Contracts.Events;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.Modules.Engagement.Slices.Achievements;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Engagement.Slices.Events;

/// <summary>Evaluates EngagementConnections achievements when an EngagementRequest becomes Connected.</summary>
internal sealed class EngagementConnectedAchievementConsumer(
    EngagementDbContext dbContext,
    AchievementProgressionCollaborator achievements
) : IIntegrationEventConsumer<EngagementRequestConnectedIntegrationEvent>
{
    public async Task ConsumeAsync(
        EngagementRequestConnectedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var types = await dbContext
            .AchievementTypes.AsNoTracking()
            .Where(static t =>
                t.IsActive && t.SourceKind == AchievementSourceKind.EngagementConnections
            )
            .ToListAsync(cancellationToken);
        if (types.Count == 0)
        {
            return;
        }

        var connectedCount = await dbContext.EngagementRequests.CountAsync(
            r =>
                r.RequesterPersonId == integrationEvent.RequesterPersonId
                && r.RequestState == EngagementRequestState.Connected,
            cancellationToken
        );
        var asOfDate = DateOnly.FromDateTime(integrationEvent.OccurredOnUtc.UtcDateTime);

        foreach (var type in types)
        {
            _ = await achievements.ApplySingleAsync(
                type,
                integrationEvent.RequesterPersonId,
                Math.Max(1, connectedCount),
                asOfDate,
                cancellationToken
            );
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
