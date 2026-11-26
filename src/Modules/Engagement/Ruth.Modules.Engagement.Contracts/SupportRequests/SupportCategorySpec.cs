namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Specification for creating or updating an organization-agnostic SupportCategory.</summary>
/// <param name="Name">Category display name (e.g., Member Assistance, Wellbeing Check-In, Community Care).</param>
/// <param name="Slug">Unique URL slug.</param>
/// <param name="Description">Optional description.</param>
/// <param name="RequiresModeration">Whether intake submissions require moderation before wall display.</param>
/// <param name="DefaultExpirationDays">Default active duration in days (1..365).</param>
/// <param name="AllowAnonymousIntake">Whether anonymous submissions are accepted.</param>
/// <param name="AllowPublicCommunityWall">Whether public wall visibility is permitted.</param>
/// <param name="AllowComments">Whether threaded comments are allowed.</param>
/// <param name="AutoFlagHoldThreshold">Number of flags that automatically pull a post into Flagged review.</param>
/// <param name="DefaultCoordinatorPersonId">Optional default care coordinator Person ID.</param>
/// <param name="IsActive">Whether active.</param>
public sealed record SupportCategorySpec(
    string Name,
    string Slug,
    string? Description = null,
    bool RequiresModeration = true,
    int DefaultExpirationDays = 14,
    bool AllowAnonymousIntake = true,
    bool AllowPublicCommunityWall = true,
    bool AllowComments = true,
    int AutoFlagHoldThreshold = 3,
    Guid? DefaultCoordinatorPersonId = null,
    bool IsActive = true
);
