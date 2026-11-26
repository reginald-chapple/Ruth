namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Specification for adding or updating a ConstituentContact in a personal portfolio.</summary>
/// <param name="OwnerPersonId">Steward, advisor, or caseworker Person identifier.</param>
/// <param name="ConstituentPersonId">Target constituent Person identifier.</param>
/// <param name="PortfolioRole">Role label (e.g., Steward, Caseworker, Advisor).</param>
/// <param name="RelationshipStrength">Initial or updated relationship strength.</param>
/// <param name="OutreachCadenceDays">Target days between regular outreach touchpoints (0 = ad-hoc).</param>
/// <param name="ConnectionNoteCadenceDays">Optional target days between personal/handwritten notes.</param>
/// <param name="PreferredChannel">Preferred outreach channel.</param>
/// <param name="BranchId">Optional Branch identifier.</param>
/// <param name="Notes">Optional portfolio context notes.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="Attributes">Optional extensible attributes.</param>
public sealed record ConstituentContactSpec(
    Guid OwnerPersonId,
    Guid ConstituentPersonId,
    string PortfolioRole = "Steward",
    ContactRelationshipStrength RelationshipStrength = ContactRelationshipStrength.Developing,
    int OutreachCadenceDays = 30,
    int? ConnectionNoteCadenceDays = 90,
    TouchpointChannel PreferredChannel = TouchpointChannel.PhoneCall,
    Guid? BranchId = null,
    string? Notes = null,
    bool IsActive = true,
    IReadOnlyDictionary<string, string?>? Attributes = null
);
