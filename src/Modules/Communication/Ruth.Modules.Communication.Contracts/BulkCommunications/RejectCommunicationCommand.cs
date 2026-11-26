using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Rejects a pending Communication and returns it for author revision.</summary>
/// <param name="CommunicationId">Identifier of the communication to reject.</param>
/// <param name="Reason">Mandatory explanation for rejection.</param>
/// <param name="ReviewerPersonId">Optional reviewer person identifier.</param>
public sealed record RejectCommunicationCommand(
    Guid CommunicationId,
    string Reason,
    Guid? ReviewerPersonId = null
) : ICommand<CommunicationDetailResponse>;
