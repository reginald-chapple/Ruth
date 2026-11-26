namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Summary of a batch dispatch execution across due approved/sending communications.</summary>
/// <param name="CommunicationsTouched">Number of communications processed in this batch.</param>
/// <param name="CommunicationsCompleted">Number of communications transitioned to <see cref="CommunicationStatus.Sent" />.</param>
/// <param name="RecipientsDelivered">Recipients successfully handed off to a transport.</param>
/// <param name="RecipientsSkipped">Recipients skipped due to unsubscribe/suppression preferences.</param>
/// <param name="RecipientsFailed">Recipients that failed rendering or transport delivery.</param>
public sealed record BulkDispatchBatchResultResponse(
    int CommunicationsTouched,
    int CommunicationsCompleted,
    int RecipientsDelivered,
    int RecipientsSkipped,
    int RecipientsFailed
);
