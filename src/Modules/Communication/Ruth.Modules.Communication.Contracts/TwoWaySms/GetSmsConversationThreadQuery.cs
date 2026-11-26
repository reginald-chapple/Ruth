using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Retrieves chronological messages in a specific two-way SMS conversation thread.</summary>
/// <param name="SystemPhoneNumberId">Organization system phone number identifier.</param>
/// <param name="ParticipantPhoneNumber">Constituent E.164 phone number.</param>
/// <param name="Limit">Maximum messages to return (1..200).</param>
public sealed record GetSmsConversationThreadQuery(
    Guid SystemPhoneNumberId,
    string ParticipantPhoneNumber,
    int Limit = 100
) : IQuery<IReadOnlyList<SmsConversationMessageResponse>>;
