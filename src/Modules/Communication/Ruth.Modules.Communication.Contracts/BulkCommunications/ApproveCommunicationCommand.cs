using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Approves a Communication so the batch dispatch engine can send it immediately or at its scheduled window.</summary>
/// <param name="CommunicationId">Identifier of the communication to approve.</param>
/// <param name="ReviewerPersonId">Optional reviewer person identifier.</param>
/// <param name="ApprovalNote">Optional reviewer approval note.</param>
public sealed record ApproveCommunicationCommand(
    Guid CommunicationId,
    Guid? ReviewerPersonId = null,
    string? ApprovalNote = null
) : ICommand<CommunicationDetailResponse>;
