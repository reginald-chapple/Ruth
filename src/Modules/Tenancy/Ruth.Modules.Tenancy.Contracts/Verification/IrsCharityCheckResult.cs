using Ruth.Modules.Tenancy.Contracts.Tenants;

namespace Ruth.Modules.Tenancy.Contracts.Verification;

/// <summary>Result of checking an Employer Identification Number against IRS EO BMF / Publication 78 data.</summary>
/// <param name="Status">The verification status.</param>
/// <param name="NormalizedEin">The normalized XX-XXXXXXX EIN.</param>
/// <param name="OfficialOrganizationName">The IRS-recorded organization name when found.</param>
/// <param name="SubsectionCode">The IRS tax-exempt subsection code (for example 501(c)(3)).</param>
/// <param name="DeductibilityCode">The IRS Publication 78 deductibility code (for example PC, PF, or GROUP).</param>
/// <param name="StateCode">The IRS-recorded two-letter state code when found.</param>
/// <param name="DiagnosticSummary">Human-readable verification summary for audit logs.</param>
public sealed record IrsCharityCheckResult(
    IrsVerificationStatus Status,
    string NormalizedEin,
    string? OfficialOrganizationName,
    string? SubsectionCode,
    string? DeductibilityCode,
    string? StateCode,
    string DiagnosticSummary
);
