namespace Ruth.Modules.Crm.Contracts.BackgroundChecks;

/// <summary>Result returned by a background check provider after initiating a screening.</summary>
public sealed record BackgroundCheckSubmissionResponse(
    string ProviderRequestId,
    BackgroundCheckStatus InitialStatus,
    string RawProviderStatus,
    string? InvitationUrl
);
