using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Domain;

/// <summary>Platform control-plane aggregate representing an isolated IRS-recognized tax-exempt organization.</summary>
public sealed class Tenant : AggregateRoot<TenantId>, IAuditableEntity, IHasConcurrencyToken
{
    private Tenant() { }

    private Tenant(
        TenantId id,
        string slug,
        string legalName,
        string displayName,
        string ein,
        string irsSubsectionCode,
        string stateCode,
        string timeZoneId,
        DateTimeOffset createdOnUtc
    )
        : base(id)
    {
        Slug = slug;
        LegalName = legalName;
        DisplayName = displayName;
        Ein = ein;
        IrsSubsectionCode = irsSubsectionCode;
        StateCode = stateCode;
        TimeZoneId = timeZoneId;
        LifecycleStage = TenantLifecycleStage.Sandbox;
        IrsStatus = IrsVerificationStatus.Unchecked;
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public string Slug { get; private set; } = string.Empty;

    public string LegalName { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string Ein { get; private set; } = string.Empty;

    public string IrsSubsectionCode { get; private set; } = string.Empty;

    public string StateCode { get; private set; } = string.Empty;

    public string TimeZoneId { get; private set; } = "UTC";

    public TenantLifecycleStage LifecycleStage { get; private set; }

    public IrsVerificationStatus IrsStatus { get; private set; }

    public string? IrsOfficialName { get; private set; }

    public string? IrsDeductibilityCode { get; private set; }

    public DateTimeOffset? IrsVerifiedOnUtc { get; private set; }

    public string? StripeConnectAccountId { get; private set; }

    public bool StripeChargesEnabled { get; private set; }

    public bool StripePayoutsEnabled { get; private set; }

    public DateTimeOffset? SubmittedForReviewOnUtc { get; private set; }

    public DateTimeOffset? ApprovedOnUtc { get; private set; }

    public string? ApprovedByOperatorEmail { get; private set; }

    public string? ReviewNotes { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public Guid ConcurrencyStamp { get; private set; }

    public static Result<Tenant> RegisterSandbox(
        string? legalName,
        string? displayName,
        string? slug,
        string? ein,
        string? irsSubsectionCode,
        string? stateCode,
        string? timeZoneId,
        DateTimeOffset nowUtc
    )
    {
        if (string.IsNullOrWhiteSpace(legalName) || legalName.Trim().Length > 200)
        {
            return DomainError.Validation(
                "Tenancy.InvalidLegalName",
                "Legal organization name is required (maximum 200 characters)."
            );
        }

        var slugResult = TenantSlugPolicy.NormalizeAndValidateSlug(slug);
        var einResult = EinFormatter.NormalizeAndValidate(ein);
        var subsectionResult = TenantSlugPolicy.NormalizeAndValidateSubsection(irsSubsectionCode);
        var combined = Result.Combine(slugResult, einResult, subsectionResult);
        if (combined.IsFailure)
        {
            return Result.Failure<Tenant>(combined.Errors);
        }

        if (
            string.IsNullOrWhiteSpace(stateCode)
            || stateCode.Trim().Length != 2
            || !char.IsAsciiLetter(stateCode.Trim()[0])
            || !char.IsAsciiLetter(stateCode.Trim()[1])
        )
        {
            return TenancyErrors.InvalidStateCode;
        }

        var normalizedDisplay = string.IsNullOrWhiteSpace(displayName)
            ? legalName.Trim()
            : displayName.Trim();
        var normalizedTz = string.IsNullOrWhiteSpace(timeZoneId) ? "UTC" : timeZoneId.Trim();

        return new Tenant(
            TenantId.Create(),
            slugResult.Value,
            legalName.Trim(),
            normalizedDisplay,
            einResult.Value,
            subsectionResult.Value,
            stateCode.Trim().ToUpperInvariant(),
            normalizedTz,
            nowUtc
        );
    }

    public void AttachStripeConnectAccount(
        string accountId,
        bool chargesEnabled,
        bool payoutsEnabled
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        StripeConnectAccountId = accountId.Trim();
        StripeChargesEnabled = chargesEnabled;
        StripePayoutsEnabled = payoutsEnabled;
    }

    public void RecordIrsVerification(
        IrsVerificationStatus status,
        string? officialName,
        string? deductibilityCode,
        DateTimeOffset verifiedOnUtc
    )
    {
        IrsStatus = status;
        IrsOfficialName = officialName;
        IrsDeductibilityCode = deductibilityCode;
        IrsVerifiedOnUtc = verifiedOnUtc.ToUniversalTime();
    }

    public Result SubmitForGoLive(DateTimeOffset nowUtc)
    {
        if (!StripeChargesEnabled || !StripePayoutsEnabled)
        {
            return TenancyErrors.StripeConnectIncomplete;
        }

        if (
            IrsStatus
            is not (
                IrsVerificationStatus.VerifiedActiveExempt
                or IrsVerificationStatus.ManualReviewRequired
            )
        )
        {
            return TenancyErrors.IrsVerificationFailed;
        }

        LifecycleStage = TenantLifecycleStage.PendingVerification;
        SubmittedForReviewOnUtc = nowUtc.ToUniversalTime();
        return Result.Success();
    }

    public Result ApproveLive(
        string operatorEmail,
        string? notes,
        bool manualIrsOverrideApproved,
        DateTimeOffset nowUtc
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operatorEmail);

        if (!StripeChargesEnabled || !StripePayoutsEnabled)
        {
            return TenancyErrors.StripeConnectIncomplete;
        }

        if (IrsStatus == IrsVerificationStatus.ManualReviewRequired && manualIrsOverrideApproved)
        {
            IrsStatus = IrsVerificationStatus.VerifiedActiveExempt;
            IrsVerifiedOnUtc = nowUtc.ToUniversalTime();
        }

        if (IrsStatus != IrsVerificationStatus.VerifiedActiveExempt)
        {
            return TenancyErrors.IrsVerificationFailed;
        }

        LifecycleStage = TenantLifecycleStage.Live;
        ApprovedOnUtc = nowUtc.ToUniversalTime();
        ApprovedByOperatorEmail = operatorEmail.Trim().ToLowerInvariant();
        ReviewNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        return Result.Success();
    }

    public void ReturnToSandbox(string operatorEmail, string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operatorEmail);
        LifecycleStage = TenantLifecycleStage.Sandbox;
        ReviewNotes = string.IsNullOrWhiteSpace(notes)
            ? $"Returned to Sandbox by {operatorEmail.Trim()}."
            : notes.Trim();
    }

    public void Suspend(string operatorEmail, string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operatorEmail);
        LifecycleStage = TenantLifecycleStage.Suspended;
        ReviewNotes = string.IsNullOrWhiteSpace(notes)
            ? $"Suspended by {operatorEmail.Trim()}."
            : notes.Trim();
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }
}
