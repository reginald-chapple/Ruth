using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.BackgroundChecks;

/// <summary>Command to verify and apply an inbound background check webhook callback.</summary>
public sealed record ProcessBackgroundCheckWebhookCommand(
    string ProviderKey,
    string RawPayload,
    string? SignatureHeader
) : ICommand<BackgroundCheckResponse>;
