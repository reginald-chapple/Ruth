using Ruth.Modules.Communication.Contracts.Transports;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Summary projection of a bulk or targeted communication campaign.</summary>
/// <param name="Id">Unique communication identifier.</param>
/// <param name="Title">Internal/campaign title.</param>
/// <param name="Channel">Target communication channel.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="TopicKey">Preference topic key.</param>
/// <param name="IsBulk">Whether bulk unsubscribe rules apply.</param>
/// <param name="Subject">Subject template for email campaigns.</param>
/// <param name="TotalRecipients">Total queued recipient count.</param>
/// <param name="DeliveredCount">Delivered recipient count.</param>
/// <param name="FailedCount">Failed recipient count.</param>
/// <param name="SkippedCount">Unsubscribed/skipped recipient count.</param>
/// <param name="BouncedCount">Bounced recipient count.</param>
/// <param name="UniqueOpenedCount">Unique recipients who opened the message.</param>
/// <param name="UniqueClickedCount">Unique recipients who clicked a link.</param>
/// <param name="ScheduledSendAtUtc">Optional scheduled send timestamp (UTC).</param>
/// <param name="SentAtUtc">Completion timestamp (UTC).</param>
/// <param name="CreatedOnUtc">Creation timestamp (UTC).</param>
public sealed record CommunicationSummaryResponse(
    Guid Id,
    string Title,
    CommunicationChannel Channel,
    CommunicationStatus Status,
    string TopicKey,
    bool IsBulk,
    string? Subject,
    int TotalRecipients,
    int DeliveredCount,
    int FailedCount,
    int SkippedCount,
    int BouncedCount,
    int UniqueOpenedCount,
    int UniqueClickedCount,
    DateTimeOffset? ScheduledSendAtUtc,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset CreatedOnUtc
);
