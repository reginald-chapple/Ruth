using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Domain;

/// <summary>Immutable audit trail record of an IRS EO BMF / Publication 78 check for a <see cref="Tenant" />.</summary>
public sealed class IrsExemptVerificationRecord : Entity<IrsVerificationRecordId>, IAuditableEntity
{
    private IrsExemptVerificationRecord() { }

    private IrsExemptVerificationRecord(
        IrsVerificationRecordId id,
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

    public static IrsExemptVerificationRecord Create(
        TenantId targetTenantId,
        string checkedEin,
        IrsVerificationStatus status,
        string? officialName,
        string? subsectionCode,
        string? deductibilityCode,
        string diagnosticSummary,
        DateTimeOffset checkedOnUtc
    )
    {
        return new IrsExemptVerificationRecord(
            IrsVerificationRecordId.Create(),
            targetTenantId,
            checkedEin,
            status,
            officialName,
            subsectionCode,
            deductibilityCode,
            diagnosticSummary,
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
