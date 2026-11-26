using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Records a conversion or step engagement event for an active CommunicationFlowInstanceRecipient.</summary>
/// <param name="FlowInstanceRecipientId">Target enrolled recipient identifier.</param>
/// <param name="GoalKind">Conversion goal kind achieved.</param>
/// <param name="ConversionValue">Optional monetary or metric value attributed.</param>
/// <param name="ReferenceKey">Optional URL, group ID, or external reference.</param>
/// <param name="Notes">Optional notes.</param>
/// <param name="RecordStepOpen">Optional flag recording a step open signal.</param>
/// <param name="RecordStepClick">Optional flag recording a step link click signal.</param>
public sealed record RecordFlowConversionCommand(
    Guid FlowInstanceRecipientId,
    FlowConversionGoalKind GoalKind = FlowConversionGoalKind.ManualOrExternalGoal,
    decimal? ConversionValue = null,
    string? ReferenceKey = null,
    string? Notes = null,
    bool RecordStepOpen = false,
    bool RecordStepClick = false
) : ICommand<CommunicationFlowConversionResponse>;
