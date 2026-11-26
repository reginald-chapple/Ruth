using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.Modules.Engagement.Domain.Touchpoints;

namespace Ruth.Modules.Engagement.Slices.Touchpoints;

internal static class TouchpointProjectionMapper
{
    public static ConstituentContactResponse ToResponse(
        ConstituentContact entity,
        IReadOnlyDictionary<Guid, (string Name, string? Email)> people
    )
    {
        var (name, email) = people.TryGetValue(entity.ConstituentPersonId, out var info)
            ? info
            : ($"Person {entity.ConstituentPersonId.ToString("D")[..8]}", null);

        return new ConstituentContactResponse(
            entity.Id.Value,
            entity.OwnerPersonId,
            entity.ConstituentPersonId,
            name,
            email,
            entity.BranchId,
            entity.PortfolioRole,
            entity.RelationshipStrength,
            entity.OutreachCadenceDays,
            entity.ConnectionNoteCadenceDays,
            entity.PreferredChannel,
            entity.LastTouchpointAtUtc,
            entity.LastConnectionNoteAtUtc,
            entity.NextScheduledTouchpointDate,
            entity.HealthScore,
            entity.HealthStatus,
            entity.Notes,
            entity.IsActive
        );
    }

    public static TouchpointResponse ToResponse(
        Touchpoint entity,
        string portfolioRole,
        IReadOnlyDictionary<Guid, (string Name, string? Email)> people
    )
    {
        var name = people.TryGetValue(entity.ConstituentPersonId, out var info)
            ? info.Name
            : $"Person {entity.ConstituentPersonId.ToString("D")[..8]}";

        return new TouchpointResponse(
            entity.Id.Value,
            entity.ConstituentContactId,
            entity.OwnerPersonId,
            entity.ConstituentPersonId,
            name,
            portfolioRole,
            entity.Kind,
            entity.Status,
            entity.Channel,
            entity.ScheduledDate,
            entity.CompletedAtUtc,
            entity.OutcomeSentiment,
            entity.Summary,
            entity.SnoozeCount
        );
    }
}
