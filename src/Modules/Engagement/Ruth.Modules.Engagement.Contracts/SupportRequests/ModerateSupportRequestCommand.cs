using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Command for a care-team moderator to approve, edit, flag, or archive a SupportRequest.</summary>
/// <param name="SupportRequestId">Target SupportRequest identifier.</param>
/// <param name="Decision">Moderation decision.</param>
/// <param name="ModeratorPersonId">Optional moderator Person identifier.</param>
/// <param name="EditedRequestText">Optional redacted/edited request text.</param>
/// <param name="OverrideVisibility">Optional overridden visibility level.</param>
/// <param name="AssignedCoordinatorPersonId">Optional assigned care coordinator Person ID.</param>
public sealed record ModerateSupportRequestCommand(
    Guid SupportRequestId,
    SupportModerationDecision Decision,
    Guid? ModeratorPersonId = null,
    string? EditedRequestText = null,
    SupportVisibilityLevel? OverrideVisibility = null,
    Guid? AssignedCoordinatorPersonId = null
) : ICommand<SupportRequestResponse>;
