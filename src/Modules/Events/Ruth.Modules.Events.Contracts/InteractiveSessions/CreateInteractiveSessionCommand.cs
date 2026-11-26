using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Command to create a live interactive session with optional initial actions.</summary>
public sealed record CreateInteractiveSessionCommand(
    string Title,
    Guid? EventItemOccurrenceId = null,
    Guid? ModeratorPersonId = null,
    IReadOnlyList<InteractiveActionSpec>? InitialActions = null
) : ICommand<InteractiveSessionDetailResponse>;
