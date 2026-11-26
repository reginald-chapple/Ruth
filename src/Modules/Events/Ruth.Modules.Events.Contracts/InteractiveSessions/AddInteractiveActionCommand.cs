using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Command to append a poll, Q&amp;A prompt, word-cloud, or slide action to an interactive session.</summary>
public sealed record AddInteractiveActionCommand(Guid SessionId, InteractiveActionSpec Spec)
    : ICommand<InteractiveActionResponse>;
