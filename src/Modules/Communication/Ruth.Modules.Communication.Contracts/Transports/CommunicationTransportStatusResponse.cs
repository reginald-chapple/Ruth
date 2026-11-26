namespace Ruth.Modules.Communication.Contracts.Transports;

/// <summary>Status summary of a registered communication transport provider.</summary>
/// <param name="TransportKey">Unique transport provider key.</param>
/// <param name="DisplayName">Human-readable provider display name.</param>
/// <param name="Channel">Communication channel served by the provider.</param>
/// <param name="IsConfigured">Whether the provider is configured for live use.</param>
/// <param name="IsActiveDefault">Whether this provider is currently selected as the active default for its channel.</param>
public sealed record CommunicationTransportStatusResponse(
    string TransportKey,
    string DisplayName,
    CommunicationChannel Channel,
    bool IsConfigured,
    bool IsActiveDefault
);
