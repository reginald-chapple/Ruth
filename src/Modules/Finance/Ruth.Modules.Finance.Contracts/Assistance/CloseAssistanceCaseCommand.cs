using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Command to close an assistance case.</summary>
public sealed record CloseAssistanceCaseCommand(Guid CaseId, string? ClosureNotes = null)
    : ICommand<AssistanceCaseResponse>;
