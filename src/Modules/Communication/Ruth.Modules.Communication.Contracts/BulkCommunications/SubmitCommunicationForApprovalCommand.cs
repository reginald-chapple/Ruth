using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Submits a draft Communication into the <see cref="CommunicationStatus.PendingApproval" /> review state.</summary>
/// <param name="CommunicationId">Identifier of the communication to submit.</param>
/// <param name="SubmittedByPersonId">Optional submitting author person identifier.</param>
public sealed record SubmitCommunicationForApprovalCommand(
    Guid CommunicationId,
    Guid? SubmittedByPersonId = null
) : ICommand<CommunicationDetailResponse>;
