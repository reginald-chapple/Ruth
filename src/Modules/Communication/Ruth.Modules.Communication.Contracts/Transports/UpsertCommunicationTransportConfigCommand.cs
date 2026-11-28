using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Transports;

/// <summary>Creates or updates per-tenant encrypted credentials for a communication transport.</summary>
/// <param name="TransportKey">Canonical lowercase transport key (e.g., smtp, sendgrid, mailgun, twilio-sms).</param>
/// <param name="Channel">Target communication channel.</param>
/// <param name="IsEnabled">Whether this transport configuration is enabled for the tenant.</param>
/// <param name="DefaultFromAddress">Optional default sender address or phone number.</param>
/// <param name="DefaultFromName">Optional default sender display name.</param>
/// <param name="Credentials">Secret provider credentials to envelope-encrypt at rest.</param>
public sealed record UpsertCommunicationTransportConfigCommand(
    string TransportKey,
    CommunicationChannel Channel,
    bool IsEnabled = true,
    string? DefaultFromAddress = null,
    string? DefaultFromName = null,
    IReadOnlyDictionary<string, string>? Credentials = null
) : ICommand<CommunicationTransportConfigResponse>;
