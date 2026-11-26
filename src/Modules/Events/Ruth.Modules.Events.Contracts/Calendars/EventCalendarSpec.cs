namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Specification for creating or updating an organizational or public event calendar.</summary>
public sealed record EventCalendarSpec(
    string Name,
    string Slug,
    string? Description = null,
    string? IconCssClass = null,
    string? ColorHex = null,
    bool IsPublic = true,
    bool IsIndexEnabled = true,
    bool IsActive = true
);
