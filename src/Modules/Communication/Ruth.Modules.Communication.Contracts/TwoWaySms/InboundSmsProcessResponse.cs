namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Outcome of ingesting an inbound SMS webhook message and evaluating active SmsPipeline rules.</summary>
/// <param name="InboundResponseId">Persisted inbound CommunicationResponse identifier.</param>
/// <param name="NormalizedFromPhone">Normalized sender E.164 phone number.</param>
/// <param name="MatchedPersonId">Optional matched CRM person identifier.</param>
/// <param name="MatchedPipelineActions">Count of SmsAction rules executed.</param>
/// <param name="AutomatedRepliesSent">Text bodies of any automated SMS replies dispatched.</param>
/// <param name="OptOutStateChanged">Whether SMS subscription state was updated (e.g., STOP/START).</param>
public sealed record InboundSmsProcessResponse(
    Guid InboundResponseId,
    string NormalizedFromPhone,
    Guid? MatchedPersonId,
    int MatchedPipelineActions,
    IReadOnlyList<string> AutomatedRepliesSent,
    bool OptOutStateChanged
);
