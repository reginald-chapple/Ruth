namespace Ruth.SharedKernel.Security;

/// <summary>Outcome of binary upload magic-byte, active-content, and polyglot inspection.</summary>
/// <param name="IsValid">Whether the upload passed security validation.</param>
/// <param name="CanonicalMimeType">Verified canonical MIME content type.</param>
/// <param name="FailureCode">Machine-readable failure code when rejected.</param>
/// <param name="FailureReason">Human-readable rejection explanation.</param>
public readonly record struct UploadValidationResult(
    bool IsValid,
    string CanonicalMimeType,
    string? FailureCode = null,
    string? FailureReason = null
)
{
    /// <summary>Creates a successful validation outcome with a canonical MIME type.</summary>
    public static UploadValidationResult Success(string canonicalMimeType)
    {
        return new(IsValid: true, canonicalMimeType);
    }

    /// <summary>Creates a rejected validation outcome.</summary>
    public static UploadValidationResult Fail(string code, string reason)
    {
        return new(IsValid: false, string.Empty, code, reason);
    }
}
