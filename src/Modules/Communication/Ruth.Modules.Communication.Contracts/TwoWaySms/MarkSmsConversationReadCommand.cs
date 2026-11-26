using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Marks all unread inbound messages in a two-way SMS conversation thread as read.</summary>
/// <param name="SystemPhoneNumberId">Organization system phone number identifier.</param>
/// <param name="ParticipantPhoneNumber">Constituent phone number.</param>
public sealed record MarkSmsConversationReadCommand(
    Guid SystemPhoneNumberId,
    string ParticipantPhoneNumber
) : ICommand<int>;
