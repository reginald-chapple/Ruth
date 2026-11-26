using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Contracts.Verification;
using Ruth.Modules.Tenancy.Domain;

namespace Ruth.Modules.Tenancy.Infrastructure.Verification;

/// <summary>
/// Deterministic clean-room IRS Exempt Organizations Business Master File (EO BMF / Publication 78)
/// verification provider supporting automated compliance checks and test vectors.
/// </summary>
internal sealed class SimulatedIrsEoBmfVerificationProvider : IIrsCharityVerificationProvider
{
    public Task<IrsCharityCheckResult> VerifyEinAsync(
        string ein,
        string claimedLegalName,
        string claimedStateCode,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedResult = EinFormatter.NormalizeAndValidate(ein);
        return normalizedResult.IsFailure
            ? Task.FromResult(
                new IrsCharityCheckResult(
                    IrsVerificationStatus.NotFoundInEoBmf,
                    ein ?? string.Empty,
                    OfficialOrganizationName: null,
                    SubsectionCode: null,
                    DeductibilityCode: null,
                    StateCode: null,
                    "EIN format failed IRS prefix or length validation."
                )
            )
            : Task.FromResult(
                EvaluateNormalizedEin(normalizedResult.Value, claimedLegalName, claimedStateCode)
            );
    }

    private static IrsCharityCheckResult EvaluateNormalizedEin(
        string normalizedEin,
        string claimedLegalName,
        string claimedStateCode
    )
    {
        var upperName = claimedLegalName.Trim().ToUpperInvariant();
        var upperState = claimedStateCode.Trim().ToUpperInvariant();

        return normalizedEin switch
        {
            _ when normalizedEin.EndsWith("0000", StringComparison.Ordinal) =>
                new IrsCharityCheckResult(
                    IrsVerificationStatus.NotFoundInEoBmf,
                    normalizedEin,
                    OfficialOrganizationName: null,
                    SubsectionCode: null,
                    DeductibilityCode: null,
                    StateCode: null,
                    "EIN was not present in the IRS EO BMF extract."
                ),
            _ when normalizedEin.EndsWith("9999", StringComparison.Ordinal) =>
                new IrsCharityCheckResult(
                    IrsVerificationStatus.RevokedOrIneligible,
                    normalizedEin,
                    upperName,
                    "501(c)(3)",
                    DeductibilityCode: null,
                    upperState,
                    "IRS Publication 78 / Auto-Revocation list indicates revoked tax-exempt status."
                ),
            _ when normalizedEin.EndsWith("7777", StringComparison.Ordinal) =>
                new IrsCharityCheckResult(
                    IrsVerificationStatus.ManualReviewRequired,
                    normalizedEin,
                    OfficialOrganizationName: null,
                    "501(c)(3)",
                    DeductibilityCode: null,
                    upperState,
                    "Newly issued determination letter requires manual operator verification."
                ),
            _ => new IrsCharityCheckResult(
                IrsVerificationStatus.VerifiedActiveExempt,
                normalizedEin,
                upperName,
                "501(c)(3)",
                "PC",
                upperState,
                "Matched active 501(c)(3) public charity in IRS EO BMF / Publication 78."
            ),
        };
    }
}
