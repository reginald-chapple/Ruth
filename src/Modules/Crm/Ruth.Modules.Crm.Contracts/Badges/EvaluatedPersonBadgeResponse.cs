namespace Ruth.Modules.Crm.Contracts.Badges;

/// <summary>Rendered badge tile evaluated for a specific person on the Person 360 profile bar.</summary>
public sealed record EvaluatedPersonBadgeResponse(
    Guid BadgeDefinitionId,
    string ComponentKey,
    string Label,
    string Value,
    string Tooltip,
    string ColorHex,
    string IconName,
    int Order
);
