using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Ingests an inbound SMS webhook message, records it in the Conversations inbox, and runs SmsPipeline responders.</summary>
/// <param name="FromPhoneNumber">Sender phone number (normalized to E.164).</param>
/// <param name="ToPhoneNumber">Destination organization phone number (normalized to E.164).</param>
/// <param name="MessageBody">Inbound SMS body text.</param>
/// <param name="ProviderMessageId">Optional upstream SMS provider message SID/ID.</param>
/// <param name="SenderDisplayName">Optional sender name hint.</param>
public sealed record ProcessInboundSmsWebhookCommand(
    string FromPhoneNumber,
    string ToPhoneNumber,
    string MessageBody,
    string? ProviderMessageId = null,
    string? SenderDisplayName = null
) : ICommand<InboundSmsProcessResponse>;
