using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Verification;

/// <summary>Command to verify a submitted identity verification code enforcing TTL and max failed attempts.</summary>
public sealed record VerifyIdentityCodeCommand(
    Guid VerificationId,
    string Code,
    int TimeLimitMinutes = 15,
    int MaxFailedAttempts = 10
) : ICommand<IdentityVerificationResultResponse>;
