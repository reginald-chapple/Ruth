using Ruth.Modules.Communication.Contracts.Transports;

namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Per-step funnel reach and conversion attribution metrics within a CommunicationFlow.</summary>
/// <param name="Order">Zero-based step order.</param>
/// <param name="StepKey">Step slug key.</param>
/// <param name="StepName">Step title.</param>
/// <param name="Channel">Delivery channel.</param>
/// <param name="DelayMinutes">Configured delay minutes.</param>
/// <param name="RecipientsReached">Recipients who reached or completed this step.</param>
/// <param name="ConversionsAttributed">Conversions attributed while at this step.</param>
public sealed record FlowStepPerformanceDto(
    int Order,
    string StepKey,
    string StepName,
    CommunicationChannel Channel,
    int DelayMinutes,
    int RecipientsReached,
    int ConversionsAttributed
);
