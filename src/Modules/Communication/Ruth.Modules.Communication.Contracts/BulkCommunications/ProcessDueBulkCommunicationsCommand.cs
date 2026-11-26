using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Executes one bounded batch dispatch pass for due approved/sending Communication campaigns.</summary>
/// <param name="MaxRecipientsPerRun">Maximum pending recipients to dispatch in this invocation (1..1000).</param>
/// <param name="SpecificCommunicationId">Optional communication filter when dispatching a specific campaign immediately.</param>
public sealed record ProcessDueBulkCommunicationsCommand(
    int MaxRecipientsPerRun = 200,
    Guid? SpecificCommunicationId = null
) : ICommand<BulkDispatchBatchResultResponse>;
