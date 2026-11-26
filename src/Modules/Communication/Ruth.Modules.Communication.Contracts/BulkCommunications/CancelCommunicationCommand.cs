using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Cancels a draft, pending, or approved Communication before completion.</summary>
/// <param name="CommunicationId">Identifier of the communication to cancel.</param>
/// <param name="Reason">Optional cancellation note.</param>
public sealed record CancelCommunicationCommand(Guid CommunicationId, string? Reason = null)
    : ICommand<CommunicationDetailResponse>;
