using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Recurring;

/// <summary>Command to verify cryptographic signature and process an inbound payment gateway webhook.</summary>
/// <param name="GatewayId">Target financial gateway configuration identifier.</param>
/// <param name="RawBody">Exact raw HTTP request body bytes as UTF-8 text.</param>
/// <param name="SignatureHeader">Cryptographic signature header supplied by the gateway.</param>
public sealed record ProcessGatewayWebhookCommand(
    Guid GatewayId,
    string RawBody,
    string? SignatureHeader
) : ICommand<GatewayWebhookProcessResponse>;
