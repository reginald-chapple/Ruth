using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Domain.Events;
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
        CreatedOnUtc = createdOnUtc;
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

        var normalizedDisplay = string.IsNullOrWhiteSpace(displayName)
            ? legalName.Trim()
            : displayName.Trim();
        if (normalizedDisplay.Length > 200)
        {
            return TenancyErrors.InvalidDisplayName;
        }

        var slugResult = TenantSlugPolicy.NormalizeAndValidateSlug(slug);
        var einResult = EinFormatter.NormalizeAndValidate(ein);
        var subsectionResult = TenantSlugPolicy.NormalizeAndValidateSubsection(irsSubsectionCode);
        var stateResult = TenantSlugPolicy.NormalizeAndValidateStateCode(stateCode);
        var tzResult = TenantSlugPolicy.NormalizeAndValidateTimeZoneId(timeZoneId);
        var combined = Result.Combine(
            slugResult,
            einResult,
            subsectionResult,
            stateResult,
            tzResult
        );
        if (combined.IsFailure)
        {
            return Result.Failure<Tenant>(combined.Errors);
        }

        var utcNow = nowUtc.ToUniversalTime();
        var tenant = new Tenant(
            TenantId.Create(),
            slugResult.Value,
            legalName.Trim(),
            normalizedDisplay,
            einResult.Value,
            subsectionResult.Value,
            stateResult.Value,
            tzResult.Value,
            utcNow
        );
        tenant.RaiseDomainEvent(
            new TenantSandboxRegisteredDomainEvent(
                Guid.CreateVersion7(),
                tenant.Id,
                tenant.Slug,
                tenant.Ein,
                utcNow
            )
        );
        return tenant;
    }

    public Result AttachStripeConnectAccount(
        string? accountId,
        bool chargesEnabled,
        bool payoutsEnabled
    )
    {
        if (string.IsNullOrWhiteSpace(accountId) || accountId.Trim().Length > 128)
        {
            return TenancyErrors.InvalidStripeAccountId;
        }

        StripeConnectAccountId = accountId.Trim();
        StripeChargesEnabled = chargesEnabled;
        StripePayoutsEnabled = payoutsEnabled;
        return Result.Success();
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
        if (LifecycleStage != TenantLifecycleStage.Sandbox)
        {
            return TenancyErrors.InvalidLifecycleTransition;
        }

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

        var utcNow = nowUtc.ToUniversalTime();
        LifecycleStage = TenantLifecycleStage.PendingVerification;
        SubmittedForReviewOnUtc = utcNow;
        RaiseDomainEvent(
            new TenantSubmittedForGoLiveDomainEvent(
                Guid.CreateVersion7(),
                Id,
                IrsStatus,
                StripeConnectAccountId ?? string.Empty,
                utcNow
            )
        );
        return Result.Success();
    }

    public Result ApproveLive(
        string? operatorEmail,
        string? notes,
        bool manualIrsOverrideApproved,
        DateTimeOffset nowUtc
    )
    {
        if (string.IsNullOrWhiteSpace(operatorEmail))
        {
            return TenancyErrors.InvalidOperatorIdentity;
        }

        if (LifecycleStage != TenantLifecycleStage.PendingVerification)
        {
            return TenancyErrors.InvalidLifecycleTransition;
        }

        if (!StripeChargesEnabled || !StripePayoutsEnabled)
        {
            return TenancyErrors.StripeConnectIncomplete;
        }

        var utcNow = nowUtc.ToUniversalTime();
        if (IrsStatus == IrsVerificationStatus.ManualReviewRequired && manualIrsOverrideApproved)
        {
            IrsStatus = IrsVerificationStatus.VerifiedActiveExempt;
            IrsVerifiedOnUtc = utcNow;
        }

        if (IrsStatus != IrsVerificationStatus.VerifiedActiveExempt)
        {
            return TenancyErrors.IrsVerificationFailed;
        }

        LifecycleStage = TenantLifecycleStage.Live;
        ApprovedOnUtc = utcNow;
        ApprovedByOperatorEmail = operatorEmail.Trim().ToLowerInvariant();
        ReviewNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        RaiseDomainEvent(
            new TenantApprovedLiveDomainEvent(
                Guid.CreateVersion7(),
                Id,
                ApprovedByOperatorEmail,
                manualIrsOverrideApproved,
                utcNow
            )
        );
        return Result.Success();
    }

    public Result ReturnToSandbox(string? operatorEmail, string? notes, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(operatorEmail))
        {
            return TenancyErrors.InvalidOperatorIdentity;
        }

        if (
            LifecycleStage
            is not (TenantLifecycleStage.PendingVerification or TenantLifecycleStage.Live)
        )
        {
            return TenancyErrors.InvalidLifecycleTransition;
        }

        var normalizedOperator = operatorEmail.Trim().ToLowerInvariant();
        LifecycleStage = TenantLifecycleStage.Sandbox;
        ApprovedOnUtc = null;
        ApprovedByOperatorEmail = null;
        ReviewNotes = string.IsNullOrWhiteSpace(notes)
            ? $"Returned to Sandbox by {normalizedOperator}."
            : notes.Trim();
        RaiseDomainEvent(
            new TenantReturnedToSandboxDomainEvent(
                Guid.CreateVersion7(),
                Id,
                normalizedOperator,
                nowUtc.ToUniversalTime()
            )
        );
        return Result.Success();
    }

    public Result Suspend(string? operatorEmail, string? notes, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(operatorEmail))
        {
            return TenancyErrors.InvalidOperatorIdentity;
        }

        if (LifecycleStage == TenantLifecycleStage.Suspended)
        {
            return TenancyErrors.InvalidLifecycleTransition;
        }

        var normalizedOperator = operatorEmail.Trim().ToLowerInvariant();
        LifecycleStage = TenantLifecycleStage.Suspended;
        ReviewNotes = string.IsNullOrWhiteSpace(notes)
            ? $"Suspended by {normalizedOperator}."
            : notes.Trim();
        RaiseDomainEvent(
            new TenantSuspendedDomainEvent(
                Guid.CreateVersion7(),
                Id,
                normalizedOperator,
                nowUtc.ToUniversalTime()
            )
        );
        return Result.Success();
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
