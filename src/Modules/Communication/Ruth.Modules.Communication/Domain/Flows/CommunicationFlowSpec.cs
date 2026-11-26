using Ruth.Modules.Communication.Contracts.Flows;

namespace Ruth.Modules.Communication.Domain.Flows;

/// <summary>Creation and update specification for <see cref="CommunicationFlow" />.</summary>
public sealed record CommunicationFlowSpec(
    string Name,
    string Key,
    CommunicationFlowCategory Category,
    IReadOnlyList<CommunicationFlowStepDto>? Steps,
    string TopicKey = "general-announcements",
    string? Description = null,
    FlowTriggerMode TriggerMode = FlowTriggerMode.RollingEnrollment,
    FlowExitCondition ExitCondition = FlowExitCondition.ExitOnConversion,
    FlowConversionGoalKind ConversionGoalKind = FlowConversionGoalKind.ManualOrExternalGoal,
    Guid? ConversionTargetGroupId = null,
    string? ConversionGoalUrlPrefix = null,
    int ConversionWindowDays = 30,
    bool IsSystem = false,
    bool IsActive = true,
    Guid? SystemGuid = null
);
