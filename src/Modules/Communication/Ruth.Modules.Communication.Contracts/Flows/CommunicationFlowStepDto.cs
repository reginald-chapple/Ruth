using Ruth.Modules.Communication.Contracts.Transports;

namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Timed step definition inside a multi-step CommunicationFlow drip campaign.</summary>
/// <param name="Order">Zero-based execution order.</param>
/// <param name="StepKey">Unique step slug within the flow (e.g., day-0-welcome).</param>
/// <param name="Name">Human-readable step title.</param>
/// <param name="Channel">Delivery channel for this step.</param>
/// <param name="DelayMinutes">Minutes to wait after enrollment (step 0) or after previous step before sending.</param>
/// <param name="SubjectTemplate">Email or push title merge template.</param>
/// <param name="BodyTemplate">HTML body, SMS body, or push body merge template.</param>
/// <param name="CommunicationTemplateId">Optional reusable CommunicationTemplate identifier.</param>
/// <param name="SendOnlyIfPreviousUnopened">When true, skips this step if the recipient already opened an earlier step.</param>
public sealed record CommunicationFlowStepDto(
    int Order,
    string StepKey,
    string Name,
    CommunicationChannel Channel,
    int DelayMinutes,
    string? SubjectTemplate = null,
    string? BodyTemplate = null,
    Guid? CommunicationTemplateId = null,
    bool SendOnlyIfPreviousUnopened = false
);
