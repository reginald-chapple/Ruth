namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Read projection for an interactive action within a session.</summary>
public sealed record InteractiveActionResponse(
    Guid Id,
    Guid SessionId,
    string Title,
    string PromptText,
    InteractiveActionType ActionType,
    IReadOnlyList<string> Options,
    int DisplayOrder,
    bool AllowMultipleResponses,
    bool RequireModeration,
    bool ShowLiveResultsOnStage,
    bool IsOpen
);
