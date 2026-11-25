namespace Ruth.Modules.Crm.Contracts.Verification;

/// <summary>Outcome of verifying an identity verification code.</summary>
public sealed record IdentityVerificationResultResponse(
    Guid VerificationId,
    bool IsVerified,
    int FailedMatchAttemptCount,
    DateTimeOffset? VerifiedOnUtc
);
