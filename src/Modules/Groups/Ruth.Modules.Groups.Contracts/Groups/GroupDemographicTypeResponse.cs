namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Demographic category definition scoped to a GroupType.</summary>
public sealed record GroupDemographicTypeResponse(
    Guid Id,
    Guid GroupTypeId,
    string Name,
    string? Description,
    string ComponentKey,
    bool IsAutomated,
    bool RunOnPersonUpdate
);
