using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Sends a 1-to-1 outbound SMS reply from staff into an active two-way SMS conversation thread.</summary>
/// <param name="SystemPhoneNumberId">Organization E.164 phone number identifier to send from.</param>
/// <param name="ParticipantPhoneNumber">Recipient constituent E.164 phone number.</param>
/// <param name="MessageBody">Plain-text SMS reply body.</param>
/// <param name="RepliedByPersonId">Optional staff person identifier.</param>
public sealed record SendSmsConversationReplyCommand(
    Guid SystemPhoneNumberId,
    string ParticipantPhoneNumber,
    string MessageBody,
    Guid? RepliedByPersonId = null
) : ICommand<SmsConversationMessageResponse>;
