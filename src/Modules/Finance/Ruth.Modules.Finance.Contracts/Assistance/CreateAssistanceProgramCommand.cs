using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Command to create a new assistance, scholarship, fee-waiver, or micro-grant program.</summary>
public sealed record CreateAssistanceProgramCommand(AssistanceProgramSpec Spec)
    : ICommand<AssistanceProgramResponse>;
