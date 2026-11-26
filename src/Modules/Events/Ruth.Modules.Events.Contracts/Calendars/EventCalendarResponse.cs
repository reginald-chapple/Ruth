namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Read projection for a configured event calendar.</summary>
public sealed record EventCalendarResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? IconCssClass,
    string? ColorHex,
    bool IsPublic,
    bool IsIndexEnabled,
    bool IsActive,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
