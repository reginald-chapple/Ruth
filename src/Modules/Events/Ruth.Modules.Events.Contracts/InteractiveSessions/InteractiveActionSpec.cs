namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Specification for adding an interactive poll, Q&amp;A prompt, word-cloud, or slide to an InteractiveSession.</summary>
public sealed record InteractiveActionSpec(
    string Title,
    string PromptText,
    InteractiveActionType ActionType,
    IReadOnlyList<string>? Options = null,
    int DisplayOrder = 0,
    bool AllowMultipleResponses = false,
    bool RequireModeration = false,
    bool ShowLiveResultsOnStage = true,
    bool IsOpen = true
);
