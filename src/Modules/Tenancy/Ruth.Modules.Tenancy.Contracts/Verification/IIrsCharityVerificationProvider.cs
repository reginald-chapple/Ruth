namespace Ruth.Modules.Tenancy.Contracts.Verification;

/// <summary>Verifies organization tax-exempt status against IRS Exempt Organizations Business Master File / Publication 78.</summary>
public interface IIrsCharityVerificationProvider
{
    /// <summary>Checks the specified EIN and expected organization attributes for active IRS tax-exempt eligibility.</summary>
    Task<IrsCharityCheckResult> VerifyEinAsync(
        string ein,
        string claimedLegalName,
        string claimedStateCode,
        CancellationToken cancellationToken = default
    );
}
