namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Calculated or manual demographic value recorded on a Group.</summary>
public sealed record GroupDemographicValueResponse(
    Guid Id,
    Guid GroupId,
    Guid GroupDemographicTypeId,
    string DemographicTypeName,
    string? Value,
    decimal? ValueAsNumeric,
    bool? ValueAsBoolean,
    Guid? ValueAsGuid,
    string? RelatedEntityTypeKey,
    Guid? RelatedEntityId,
    DateTimeOffset? LastCalculatedOnUtc
);
