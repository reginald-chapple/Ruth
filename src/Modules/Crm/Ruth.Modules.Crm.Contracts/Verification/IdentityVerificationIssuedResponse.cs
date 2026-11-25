namespace Ruth.Modules.Crm.Contracts.Verification;

/// <summary>Result of issuing an identity verification challenge (the secret code is delivered out-of-band, never stored in public query DTOs).</summary>
public sealed record IdentityVerificationIssuedResponse(
    Guid VerificationId,
    string ReferenceNumber,
    DateTimeOffset IssuedOnUtc,
    DateTimeOffset ExpiresOnUtc,
    string DispatchedCodeForTransport
);
