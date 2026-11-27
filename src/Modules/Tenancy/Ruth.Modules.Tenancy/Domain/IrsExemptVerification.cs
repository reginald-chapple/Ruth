using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Domain;

/// <summary>Immutable audit trail record of an IRS EO BMF / Publication 78 check for a <see cref="Tenant" />.</summary>
public sealed class IrsExemptVerification : Entity<IrsExemptVerificationId>, IAuditableEntity
{
    private IrsExemptVerification() { }

    private IrsExemptVerification(
        IrsExemptVerificationId id,
        TenantId targetTenantId,
        string checkedEin,
        IrsVerificationStatus status,
        string? officialName,
        string? subsectionCode,
        string? deductibilityCode,
        string diagnosticSummary,
        DateTimeOffset checkedOnUtc
    )
        : base(id)
    {
        TargetTenantId = targetTenantId;
        CheckedEin = checkedEin;
        Status = status;
        OfficialName = officialName;
        SubsectionCode = subsectionCode;
        DeductibilityCode = deductibilityCode;
        DiagnosticSummary = diagnosticSummary;
        CreatedOnUtc = checkedOnUtc.ToUniversalTime();
    }

    public TenantId TargetTenantId { get; private set; }

    public string CheckedEin { get; private set; } = string.Empty;

    public IrsVerificationStatus Status { get; private set; }

    public string? OfficialName { get; private set; }

    public string? SubsectionCode { get; private set; }

    public string? DeductibilityCode { get; private set; }

    public string DiagnosticSummary { get; private set; } = string.Empty;

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public static Result<IrsExemptVerification> Create(
        TenantId targetTenantId,
        string? checkedEin,
        IrsVerificationStatus status,
        string? officialName,
        string? subsectionCode,
        string? deductibilityCode,
        string? diagnosticSummary,
        DateTimeOffset checkedOnUtc
    )
    {
        if (targetTenantId.IsEmpty)
        {
            return TenancyErrors.TenantNotFound;
        }

        if (string.IsNullOrWhiteSpace(checkedEin))
        {
            return TenancyErrors.InvalidEin;
        }

        var summary = string.IsNullOrWhiteSpace(diagnosticSummary)
            ? "IRS EO BMF verification recorded."
            : diagnosticSummary.Trim();

        return new IrsExemptVerification(
            IrsExemptVerificationId.Create(),
            targetTenantId,
            checkedEin.Trim(),
            status,
            string.IsNullOrWhiteSpace(officialName) ? null : officialName.Trim(),
            string.IsNullOrWhiteSpace(subsectionCode) ? null : subsectionCode.Trim(),
            string.IsNullOrWhiteSpace(deductibilityCode) ? null : deductibilityCode.Trim(),
            summary,
            checkedOnUtc
        );
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }
}
