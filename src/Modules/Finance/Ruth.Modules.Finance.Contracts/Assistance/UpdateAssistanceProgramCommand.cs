using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Command to update an existing assistance or grant program.</summary>
public sealed record UpdateAssistanceProgramCommand(Guid ProgramId, AssistanceProgramSpec Spec)
    : ICommand<AssistanceProgramResponse>;
