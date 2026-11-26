using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.VirtualRooms.Contracts.Webhooks;

/// <summary>Verifies and processes an incoming LiveKit webhook (participant_joined, participant_left, room_finished, egress_ended).</summary>
/// <param name="AuthorizationHeader">HTTP Authorization header carrying the signed LiveKit webhook JWT.</param>
/// <param name="RawPayloadJson">Raw UTF-8 JSON webhook payload.</param>
public sealed record LiveKitWebhookIngestCommand(string? AuthorizationHeader, string RawPayloadJson)
    : ICommand<LiveKitWebhookIngestResponse>;
