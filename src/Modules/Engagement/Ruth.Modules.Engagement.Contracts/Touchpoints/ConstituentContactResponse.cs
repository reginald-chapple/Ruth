namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Represents a ConstituentContact in a steward/caseworker portfolio.</summary>
/// <param name="Id">ConstituentContact identifier.</param>
/// <param name="OwnerPersonId">Portfolio owner Person identifier.</param>
/// <param name="ConstituentPersonId">Constituent Person identifier.</param>
/// <param name="ConstituentDisplayName">Constituent display name.</param>
/// <param name="ConstituentEmail">Optional constituent email.</param>
/// <param name="BranchId">Optional Branch identifier.</param>
/// <param name="PortfolioRole">Role label.</param>
/// <param name="RelationshipStrength">Qualitative relationship strength.</param>
/// <param name="OutreachCadenceDays">Outreach cadence in days.</param>
/// <param name="ConnectionNoteCadenceDays">Optional connection note cadence in days.</param>
/// <param name="PreferredChannel">Preferred outreach channel.</param>
/// <param name="LastTouchpointAtUtc">Optional last completed touchpoint timestamp.</param>
/// <param name="LastConnectionNoteAtUtc">Optional last completed note timestamp.</param>
/// <param name="NextScheduledTouchpointDate">Next scheduled cadence date.</param>
/// <param name="HealthScore">Computed relationship health score (0..100).</param>
/// <param name="HealthStatus">Computed cadence health status.</param>
/// <param name="Notes">Optional notes.</param>
/// <param name="IsActive">Whether active.</param>
public sealed record ConstituentContactResponse(
    Guid Id,
    Guid OwnerPersonId,
    Guid ConstituentPersonId,
    string ConstituentDisplayName,
    string? ConstituentEmail,
    Guid? BranchId,
    string PortfolioRole,
    ContactRelationshipStrength RelationshipStrength,
    int OutreachCadenceDays,
    int? ConnectionNoteCadenceDays,
    TouchpointChannel PreferredChannel,
    DateTimeOffset? LastTouchpointAtUtc,
    DateTimeOffset? LastConnectionNoteAtUtc,
    DateOnly? NextScheduledTouchpointDate,
    int HealthScore,
    ConstituentHealthStatus HealthStatus,
    string? Notes,
    bool IsActive
);
