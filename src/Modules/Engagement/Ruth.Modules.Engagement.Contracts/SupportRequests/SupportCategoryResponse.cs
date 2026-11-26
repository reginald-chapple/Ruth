namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Represents a configured SupportCategory.</summary>
/// <param name="Id">Category identifier.</param>
/// <param name="Name">Category name.</param>
/// <param name="Slug">Unique URL slug.</param>
/// <param name="Description">Optional description.</param>
/// <param name="RequiresModeration">Whether moderation is required.</param>
/// <param name="DefaultExpirationDays">Default expiration days.</param>
/// <param name="AllowAnonymousIntake">Whether anonymous intake is allowed.</param>
/// <param name="AllowPublicCommunityWall">Whether public wall sharing is allowed.</param>
/// <param name="AllowComments">Whether comments are allowed.</param>
/// <param name="AutoFlagHoldThreshold">Auto-flag hold threshold.</param>
/// <param name="DefaultCoordinatorPersonId">Optional default care coordinator ID.</param>
/// <param name="IsActive">Whether active.</param>
public sealed record SupportCategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    bool RequiresModeration,
    int DefaultExpirationDays,
    bool AllowAnonymousIntake,
    bool AllowPublicCommunityWall,
    bool AllowComments,
    int AutoFlagHoldThreshold,
    Guid? DefaultCoordinatorPersonId,
    bool IsActive
);
