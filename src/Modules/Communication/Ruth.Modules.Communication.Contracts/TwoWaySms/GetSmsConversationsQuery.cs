using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Lists two-way SMS inbox conversations ordered by most recent activity.</summary>
/// <param name="SystemPhoneNumberId">Optional system phone number filter.</param>
/// <param name="UnreadOnly">Whether to return only threads with unread inbound messages.</param>
/// <param name="Limit">Maximum conversation threads to return (1..100).</param>
public sealed record GetSmsConversationsQuery(
    Guid? SystemPhoneNumberId = null,
    bool UnreadOnly = false,
    int Limit = 50
) : IQuery<IReadOnlyList<SmsConversationSummaryResponse>>;
