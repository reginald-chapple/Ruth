namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Represents a reusable schema template for CMS content channels.</summary>
public sealed record ContentChannelTypeResponse(
    Guid Id,
    string Name,
    string Key,
    ContentChannelDateType DateRangeType,
    bool DisablePriority,
    bool DisableStatus,
    bool IncludeTime,
    bool IsSystem
);
