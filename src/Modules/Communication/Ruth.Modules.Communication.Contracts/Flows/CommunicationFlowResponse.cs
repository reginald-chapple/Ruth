namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Full definition projection of an automated CommunicationFlow drip campaign.</summary>
/// <param name="Id">Unique flow identifier.</param>
/// <param name="Name">Flow display name.</param>
/// <param name="Key">Unique slug key.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Category">Flow category.</param>
/// <param name="TopicKey">Subscription preference topic key.</param>
/// <param name="TriggerMode">Enrollment trigger mode.</param>
/// <param name="ExitCondition">Early-exit rule.</param>
/// <param name="ConversionGoalKind">Conversion goal kind.</param>
/// <param name="ConversionTargetGroupId">Optional target group identifier when goal is JoinedTargetGroup.</param>
/// <param name="ConversionGoalUrlPrefix">Optional goal URL prefix when goal is ClickedGoalUrl.</param>
/// <param name="ConversionWindowDays">Attribution window in days (1..365).</param>
/// <param name="Steps">Ordered timed steps.</param>
/// <param name="IsSystem">Whether built-in and protected from deletion.</param>
/// <param name="IsActive">Whether active for enrollment and step execution.</param>
/// <param name="CreatedOnUtc">Creation timestamp (UTC).</param>
/// <param name="ModifiedOnUtc">Last modification timestamp (UTC).</param>
public sealed record CommunicationFlowResponse(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    CommunicationFlowCategory Category,
    string TopicKey,
    FlowTriggerMode TriggerMode,
    FlowExitCondition ExitCondition,
    FlowConversionGoalKind ConversionGoalKind,
    Guid? ConversionTargetGroupId,
    string? ConversionGoalUrlPrefix,
    int ConversionWindowDays,
    IReadOnlyList<CommunicationFlowStepDto> Steps,
    bool IsSystem,
    bool IsActive,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
