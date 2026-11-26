namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Submitted audience poll choice or Q&amp;A message.</summary>
public sealed record InteractiveResponseDto(
    Guid Id,
    Guid SessionId,
    Guid ActionId,
    Guid? OccurrenceId,
    Guid? ResponderPersonId,
    string? SelectedOption,
    string? TextResponse,
    InteractiveModerationStatus ModerationStatus,
    int Upvotes,
    DateTimeOffset SubmittedAtUtc
);
