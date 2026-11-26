using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Command for an audience member to submit a poll choice or Q&amp;A question.</summary>
public sealed record SubmitInteractiveResponseCommand(
    string SessionIdOrJoinCode,
    Guid ActionId,
    string ParticipantToken,
    string? SelectedOption = null,
    string? TextResponse = null,
    Guid? ResponderPersonId = null
) : ICommand<InteractiveStageProjectionResponse>;
