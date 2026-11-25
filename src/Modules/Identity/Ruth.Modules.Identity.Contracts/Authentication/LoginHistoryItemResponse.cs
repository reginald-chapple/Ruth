namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Represents a recorded authentication attempt in login history.</summary>
public sealed record LoginHistoryItemResponse(
    Guid Id,
    Guid? UserAccountId,
    string UserNameAttempted,
    string AuthMethod,
    string? ProviderKey,
    bool Succeeded,
    string? FailureReason,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset OccurredOnUtc
);
