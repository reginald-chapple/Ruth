namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Funnel and conversion analytics summary for a CommunicationFlow drip campaign.</summary>
/// <param name="FlowId">Flow identifier.</param>
/// <param name="FlowName">Flow name.</param>
/// <param name="FlowKey">Flow slug key.</param>
/// <param name="TotalInstances">Launched cohort instance count.</param>
/// <param name="TotalEnrolled">Total enrolled recipients across instances.</param>
/// <param name="ActiveInFlight">Recipients currently active in the drip sequence.</param>
/// <param name="CompletedAllSteps">Recipients who completed every step.</param>
/// <param name="ConvertedExitedCount">Recipients who exited early after conversion/engagement.</param>
/// <param name="UnsubscribedExitedCount">Recipients who exited due to opt-out.</param>
/// <param name="TotalConversions">Total conversion events recorded.</param>
/// <param name="ConversionRatePercent">Percentage of enrolled recipients with at least one conversion (0..100).</param>
/// <param name="TotalConversionValue">Sum of attributed conversion values.</param>
/// <param name="OpenRatePercent">Percentage of enrolled recipients with at least one step open (0..100).</param>
/// <param name="ClickRatePercent">Percentage of enrolled recipients with at least one step click (0..100).</param>
/// <param name="StepFunnel">Ordered per-step reach and conversion metrics.</param>
public sealed record CommunicationFlowAnalyticsResponse(
    Guid FlowId,
    string FlowName,
    string FlowKey,
    int TotalInstances,
    int TotalEnrolled,
    int ActiveInFlight,
    int CompletedAllSteps,
    int ConvertedExitedCount,
    int UnsubscribedExitedCount,
    int TotalConversions,
    decimal ConversionRatePercent,
    decimal TotalConversionValue,
    decimal OpenRatePercent,
    decimal ClickRatePercent,
    IReadOnlyList<FlowStepPerformanceDto> StepFunnel
);
