using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Command for a moderator to approve, hold, hide, or upvote an audience Q&amp;A response.</summary>
public sealed record ModerateInteractiveResponseCommand(
    Guid SessionId,
    Guid ResponseId,
    InteractiveModerationStatus ModerationStatus,
    bool IncrementUpvote = false
) : ICommand<InteractiveStageProjectionResponse>;
