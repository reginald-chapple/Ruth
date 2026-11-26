namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Represents a Community Care / SupportRequest item.</summary>
/// <param name="Id">SupportRequest identifier.</param>
/// <param name="SupportCategoryId">Category identifier.</param>
/// <param name="CategoryName">Category display name.</param>
/// <param name="RequesterPersonId">Optional requester Person identifier.</param>
/// <param name="FirstName">Requester first name (or Anonymous).</param>
/// <param name="LastName">Optional requester last name (redacted on public wall).</param>
/// <param name="Email">Optional requester email (redacted on public wall).</param>
/// <param name="BranchId">Optional Branch identifier.</param>
/// <param name="AssignedCareCoordinatorPersonId">Optional assigned care coordinator ID.</param>
/// <param name="RequestText">Support request text.</param>
/// <param name="AnswerOrOutcomeSummary">Optional outcome or praise/resolution summary.</param>
/// <param name="VisibilityLevel">Configured visibility scope.</param>
/// <param name="ModerationState">Current moderation state.</param>
/// <param name="IsUrgent">Whether flagged urgent.</param>
/// <param name="AllowComments">Whether comments are enabled.</param>
/// <param name="CommunitySupportCount">Count of community/care-team support actions.</param>
/// <param name="FlagCount">Moderation flag count.</param>
/// <param name="EnteredAtUtc">Submission UTC timestamp.</param>
/// <param name="ExpiresAtUtc">Expiration UTC timestamp.</param>
/// <param name="ResolvedAtUtc">Optional resolution UTC timestamp.</param>
/// <param name="Comments">Threaded comments.</param>
public sealed record SupportRequestResponse(
    Guid Id,
    Guid SupportCategoryId,
    string CategoryName,
    Guid? RequesterPersonId,
    string FirstName,
    string? LastName,
    string? Email,
    Guid? BranchId,
    Guid? AssignedCareCoordinatorPersonId,
    string RequestText,
    string? AnswerOrOutcomeSummary,
    SupportVisibilityLevel VisibilityLevel,
    SupportModerationState ModerationState,
    bool IsUrgent,
    bool AllowComments,
    int CommunitySupportCount,
    int FlagCount,
    DateTimeOffset EnteredAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    IReadOnlyList<SupportRequestCommentResponse> Comments
);
