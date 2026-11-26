namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Complete moderator projection for an interactive session including actions, occurrences, and moderation queue.</summary>
public sealed record InteractiveSessionDetailResponse(
    Guid Id,
    string Title,
    string JoinCode,
    Guid? EventItemOccurrenceId,
    Guid? ModeratorPersonId,
    InteractiveSessionStatus Status,
    Guid? ActiveActionId,
    Guid? ActiveOccurrenceId,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    IReadOnlyList<InteractiveActionResponse> Actions,
    IReadOnlyList<InteractiveSessionOccurrenceResponse> Occurrences,
    IReadOnlyList<InteractiveResponseDto> AllResponses
);
