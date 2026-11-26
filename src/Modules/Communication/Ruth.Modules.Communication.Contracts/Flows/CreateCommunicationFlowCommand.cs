using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Creates a multi-step CommunicationFlow drip campaign definition.</summary>
/// <param name="Name">Flow display name.</param>
/// <param name="Key">Unique slug key.</param>
/// <param name="Category">Flow category.</param>
/// <param name="Steps">Ordered timed drip steps (1..25).</param>
/// <param name="TopicKey">Preference topic key.</param>
/// <param name="Description">Optional description.</param>
/// <param name="TriggerMode">Enrollment trigger mode.</param>
/// <param name="ExitCondition">Early-exit rule.</param>
/// <param name="ConversionGoalKind">Conversion goal kind.</param>
/// <param name="ConversionTargetGroupId">Optional target group identifier.</param>
/// <param name="ConversionGoalUrlPrefix">Optional URL prefix for click conversions.</param>
/// <param name="ConversionWindowDays">Attribution window in days (1..365).</param>
/// <param name="IsActive">Whether active.</param>
public sealed record CreateCommunicationFlowCommand(
    string Name,
    string Key,
    CommunicationFlowCategory Category,
    IReadOnlyList<CommunicationFlowStepDto> Steps,
    string TopicKey = "general-announcements",
    string? Description = null,
    FlowTriggerMode TriggerMode = FlowTriggerMode.RollingEnrollment,
    FlowExitCondition ExitCondition = FlowExitCondition.ExitOnConversion,
    FlowConversionGoalKind ConversionGoalKind = FlowConversionGoalKind.ManualOrExternalGoal,
    Guid? ConversionTargetGroupId = null,
    string? ConversionGoalUrlPrefix = null,
    int ConversionWindowDays = 30,
    bool IsActive = true
) : ICommand<CommunicationFlowResponse>;
