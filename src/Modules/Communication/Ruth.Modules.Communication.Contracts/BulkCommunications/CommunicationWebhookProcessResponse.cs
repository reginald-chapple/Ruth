namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Result summary returned after processing communication delivery and engagement webhooks.</summary>
/// <param name="ProcessedCount">Number of webhook events matched and applied.</param>
/// <param name="UnmatchedCount">Number of webhook events that did not match a recipient record.</param>
/// <param name="SuppressionsRecorded">Number of new hard-bounce, complaint, or unsubscribe suppressions recorded.</param>
public sealed record CommunicationWebhookProcessResponse(
    int ProcessedCount,
    int UnmatchedCount,
    int SuppressionsRecorded
);
