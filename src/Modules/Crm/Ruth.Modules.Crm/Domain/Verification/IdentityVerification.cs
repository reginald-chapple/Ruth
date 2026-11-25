using System.Security.Cryptography;
using System.Text;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Domain.Verification;

/// <summary>Tracks an identity verification challenge with expiration, IP rate-limiting metadata, and failed-attempt lockout.</summary>
public sealed class IdentityVerification : AggregateRoot<IdentityVerificationId>, IAuditableEntity
{
    /// <summary>Default maximum allowed failed code match attempts before challenge lockout.</summary>
    public const int DefaultMaxFailedMatchAttemptCount = 10;

    private IdentityVerification() { }

    private IdentityVerification(
        IdentityVerificationId id,
        string referenceNumber,
        string requestIpAddress,
        IdentityVerificationCodeId verificationCodeId,
        DateTimeOffset issuedOnUtc
    )
        : base(id)
    {
        ReferenceNumber = referenceNumber;
        RequestIpAddress = requestIpAddress;
        VerificationCodeId = verificationCodeId;
        IssuedOnUtc = issuedOnUtc.ToUniversalTime();
        FailedMatchAttemptCount = 0;
    }

    /// <summary>Gets the target phone number or contact reference.</summary>
    public string ReferenceNumber { get; private set; } = string.Empty;

    /// <summary>Gets the UTC timestamp when this challenge was issued.</summary>
    public DateTimeOffset IssuedOnUtc { get; private set; }

    /// <summary>Gets the requester IP address used for daily rate limiting.</summary>
    public string RequestIpAddress { get; private set; } = string.Empty;

    /// <summary>Gets the linked verification code entity identifier.</summary>
    public IdentityVerificationCodeId VerificationCodeId { get; private set; }

    /// <summary>Gets the count of failed verification attempts.</summary>
    public int FailedMatchAttemptCount { get; private set; }

    /// <summary>Gets whether this challenge has been successfully verified.</summary>
    public bool IsVerified { get; private set; }

    /// <summary>Gets the UTC timestamp when verification succeeded.</summary>
    public DateTimeOffset? VerifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <summary>Creates a validated identity verification challenge.</summary>
    public static Result<IdentityVerification> Create(
        string referenceNumber,
        string requestIpAddress,
        IdentityVerificationCodeId verificationCodeId,
        DateTimeOffset issuedOnUtc
    )
    {
        return
            string.IsNullOrWhiteSpace(referenceNumber)
            || string.IsNullOrWhiteSpace(requestIpAddress)
            ? (Result<IdentityVerification>)CrmErrors.IdentityVerificationReferenceRequired
            : Result.Success(
                new IdentityVerification(
                    IdentityVerificationId.Create(),
                    referenceNumber.Trim(),
                    requestIpAddress.Trim(),
                    verificationCodeId,
                    issuedOnUtc
                )
            );
    }

    /// <summary>Verifies <paramref name="submittedCode" /> against <paramref name="expectedCode" /> using constant-time comparison.</summary>
    public Result VerifyCode(
        string submittedCode,
        string expectedCode,
        int timeLimitMinutes,
        int maxFailedAttempts,
        DateTimeOffset nowUtc
    )
    {
        var now = nowUtc.ToUniversalTime();
        var effectiveMax = Math.Max(1, maxFailedAttempts);
        var effectiveTtl = Math.Max(1, timeLimitMinutes);

        if (IssuedOnUtc.AddMinutes(effectiveTtl) < now || FailedMatchAttemptCount >= effectiveMax)
        {
            return CrmErrors.IdentityVerificationExpiredOrLocked;
        }

        var trimmed = submittedCode?.Trim() ?? string.Empty;
        var matches = ConstantTimeEquals(trimmed, expectedCode);
        if (!matches)
        {
            FailedMatchAttemptCount++;
            return CrmErrors.IdentityVerificationCodeMismatch;
        }

        IsVerified = true;
        VerifiedOnUtc = now;
        return Result.Success();
    }

    private static bool ConstantTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }
}
