using System.Globalization;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Domain.Invoices;
using Ruth.Modules.Finance.Domain.ValueObjects;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Finance.Domain.Assistance;

/// <summary>Case management aggregate for community aid, scholarships, fee waivers, and micro-grants.</summary>
public sealed class AssistanceCase
    : AggregateRoot<AssistanceCaseId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    /// <summary>Canonical entity type key for ACL evaluation and entity registry.</summary>
    public const string EntityTypeKeyName = "Ruth.Finance.AssistanceCase";

    private readonly List<AssistanceDocument> _documents = [];
    private readonly List<AssistanceDisbursement> _disbursements = [];
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private AssistanceCase() { }

    private AssistanceCase(
        AssistanceCaseId id,
        string caseNumber,
        AssistanceProgramId programId,
        Guid applicantPersonId,
        Guid? householdId,
        Guid? assignedCaseworkerPersonId,
        Guid? branchId,
        string summary,
        string? requestDetails,
        string currencyCode,
        decimal requestedAmount,
        DateTimeOffset submittedAtUtc
    )
        : base(id)
    {
        CaseNumber = caseNumber;
        ProgramId = programId;
        ApplicantPersonId = applicantPersonId;
        HouseholdId = householdId;
        AssignedCaseworkerPersonId = assignedCaseworkerPersonId;
        BranchId = branchId;
        Status = AssistanceCaseStatus.Submitted;
        Summary = summary;
        RequestDetails = requestDetails;
        CurrencyCode = currencyCode;
        RequestedAmount = requestedAmount;
        SubmittedAtUtc = submittedAtUtc.ToUniversalTime();
    }

    public string CaseNumber { get; private set; } = string.Empty;
    public AssistanceProgramId ProgramId { get; private set; }
    public Guid ApplicantPersonId { get; private set; }
    public Guid? HouseholdId { get; private set; }
    public Guid? AssignedCaseworkerPersonId { get; private set; }
    public Guid? BranchId { get; private set; }
    public AssistanceCaseStatus Status { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string? RequestDetails { get; private set; }
    public string? DecisionNotes { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal RequestedAmount { get; private set; }
    public decimal ApprovedAmount { get; private set; }
    public decimal DisbursedAmount { get; private set; }
    public decimal RemainingApprovedAmount => Math.Max(0m, ApprovedAmount - DisbursedAmount);
    public DateTimeOffset SubmittedAtUtc { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public IReadOnlyCollection<AssistanceDocument> Documents => _documents.AsReadOnly();
    public IReadOnlyCollection<AssistanceDisbursement> Disbursements => _disbursements.AsReadOnly();

    public IReadOnlyDictionary<string, string?> AttributeValues
    {
        get => _attributeValues;
        private set =>
            _attributeValues = new Dictionary<string, string?>(
                value,
                StringComparer.OrdinalIgnoreCase
            );
    }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
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

    public void SetAttributeValue(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _attributeValues[key.Trim()] = value;
    }

    public void RemoveAttributeValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _ = _attributeValues.Remove(key.Trim());
    }

    /// <summary>Submits a validated <see cref="AssistanceCase" /> intake record.</summary>
    public static Result<AssistanceCase> Submit(
        AssistanceProgramId programId,
        Guid applicantPersonId,
        decimal requestedAmount,
        string currencyCode,
        string summary,
        DateTimeOffset submittedAtUtc,
        string? requestDetails = null,
        Guid? householdId = null,
        Guid? branchId = null,
        Guid? assignedCaseworkerPersonId = null,
        IReadOnlyDictionary<string, string?>? attributes = null
    )
    {
        if (programId.IsEmpty || applicantPersonId == Guid.Empty)
        {
            return FinanceErrors.AssistanceCaseInvalid;
        }

        if (string.IsNullOrWhiteSpace(summary) || summary.Trim().Length > 200)
        {
            return FinanceErrors.AssistanceCaseInvalid;
        }

        var validMoney = Money.CreatePositive(requestedAmount, currencyCode);
        if (validMoney.IsFailure)
        {
            return Result.Failure<AssistanceCase>(validMoney.Errors);
        }

        var id = AssistanceCaseId.Create();
        var caseNumber = string.Create(
            CultureInfo.InvariantCulture,
            $"CASE-{submittedAtUtc.UtcDateTime:yyyyMMdd}-{id.Value.ToString("N")[^8..].ToUpperInvariant()}"
        );

        var entity = new AssistanceCase(
            id,
            caseNumber,
            programId,
            applicantPersonId,
            NormalizeGuid(householdId),
            NormalizeGuid(assignedCaseworkerPersonId),
            NormalizeGuid(branchId),
            summary.Trim(),
            TrimOptional(requestDetails, 2000),
            validMoney.Value.CurrencyCode,
            validMoney.Value.Amount,
            submittedAtUtc
        );
        entity.ApplyAttributes(attributes);
        return entity;
    }

    /// <summary>Attaches a supporting verification document to this case.</summary>
    public Result<AssistanceDocument> AttachDocument(
        string title,
        string documentCategory,
        Guid? binaryFileId,
        Guid? uploadedByPersonId,
        bool isVerified,
        DateTimeOffset uploadedAtUtc
    )
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(documentCategory))
        {
            return FinanceErrors.AssistanceDocumentInvalid;
        }

        var doc = new AssistanceDocument(
            AssistanceDocumentId.Create(),
            Id,
            title.Trim(),
            documentCategory.Trim(),
            NormalizeGuid(binaryFileId),
            NormalizeGuid(uploadedByPersonId),
            isVerified,
            uploadedAtUtc
        );
        _documents.Add(doc);
        return doc;
    }

    /// <summary>Places the case under review, approves an award ceiling, or denies the request.</summary>
    public Result ApplyReviewDecision(
        AssistanceCaseStatus decisionStatus,
        decimal? approvedAmount,
        Guid? assignedCaseworkerPersonId,
        string? decisionNotes,
        DateTimeOffset reviewedAtUtc,
        bool programRequiresVerifiedDocs,
        decimal maxAwardPerCase,
        decimal remainingProgramBudget
    )
    {
        if (!CanReviewInCurrentState(decisionStatus))
        {
            return FinanceErrors.AssistanceCaseInvalidTransition;
        }

        if (assignedCaseworkerPersonId is { } cw && cw != Guid.Empty)
        {
            AssignedCaseworkerPersonId = cw;
        }

        DecisionNotes = TrimOptional(decisionNotes, 1500);
        ReviewedAtUtc = reviewedAtUtc.ToUniversalTime();

        if (decisionStatus != AssistanceCaseStatus.Approved)
        {
            Status = decisionStatus;
            return Result.Success();
        }

        return ApproveAward(
            approvedAmount ?? RequestedAmount,
            programRequiresVerifiedDocs,
            maxAwardPerCase,
            remainingProgramBudget
        );
    }

    /// <summary>Records an approved disbursement or internal invoice fee-waiver credit.</summary>
    public Result<AssistanceDisbursement> RecordDisbursement(
        AssistanceDisbursementMethod method,
        decimal amount,
        string payeeName,
        InvoiceId? targetInvoiceId,
        string? referenceNumber,
        string? notes,
        Guid? approvedByPersonId,
        DateTimeOffset disbursedAtUtc
    )
    {
        var check = ValidateDisbursementPreconditions(payeeName, amount, out var validAmount);
        if (check.IsFailure)
        {
            return Result.Failure<AssistanceDisbursement>(check.Errors);
        }

        DisbursedAmount += validAmount;
        Status =
            DisbursedAmount >= ApprovedAmount
                ? AssistanceCaseStatus.Disbursed
                : AssistanceCaseStatus.PartiallyDisbursed;

        var disbursement = new AssistanceDisbursement(
            AssistanceDisbursementId.Create(),
            Id,
            method,
            validAmount,
            CurrencyCode,
            payeeName.Trim(),
            NormalizeInvoiceId(targetInvoiceId),
            TrimOptional(referenceNumber, 80),
            TrimOptional(notes, 500),
            NormalizeGuid(approvedByPersonId),
            disbursedAtUtc
        );
        _disbursements.Add(disbursement);
        return disbursement;
    }

    private Result ValidateDisbursementPreconditions(
        string payeeName,
        decimal amount,
        out decimal validAmount
    )
    {
        validAmount = 0m;
        if (
            Status is not (AssistanceCaseStatus.Approved or AssistanceCaseStatus.PartiallyDisbursed)
        )
        {
            return FinanceErrors.AssistanceCaseInvalidTransition;
        }

        if (string.IsNullOrWhiteSpace(payeeName))
        {
            return FinanceErrors.AssistanceDisbursementInvalid;
        }

        var validMoney = Money.CreatePositive(amount, CurrencyCode);
        if (validMoney.IsFailure)
        {
            return validMoney;
        }

        validAmount = validMoney.Value.Amount;
        return DisbursedAmount + validAmount > ApprovedAmount
            ? FinanceErrors.AssistanceDisbursementExceedsApproved
            : Result.Success();
    }

    private static InvoiceId? NormalizeInvoiceId(InvoiceId? targetInvoiceId)
    {
        return targetInvoiceId is { IsEmpty: false } invId ? invId : null;
    }

    /// <summary>Administratively closes the assistance case.</summary>
    public Result Close(string? closureNotes, DateTimeOffset closedAtUtc)
    {
        if (Status == AssistanceCaseStatus.Closed)
        {
            return FinanceErrors.AssistanceCaseInvalidTransition;
        }

        Status = AssistanceCaseStatus.Closed;
        ClosedAtUtc = closedAtUtc.ToUniversalTime();
        if (!string.IsNullOrWhiteSpace(closureNotes))
        {
            DecisionNotes = string.IsNullOrWhiteSpace(DecisionNotes)
                ? closureNotes.Trim()
                : $"{DecisionNotes} | Closed: {closureNotes.Trim()}";
        }

        return Result.Success();
    }

    /// <summary>Repoints person references when duplicate CRM records are merged.</summary>
    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (survivingPersonId == Guid.Empty)
        {
            return false;
        }

        var changed = false;
        if (ApplicantPersonId == retiredPersonId)
        {
            ApplicantPersonId = survivingPersonId;
            changed = true;
        }

        if (AssignedCaseworkerPersonId == retiredPersonId)
        {
            AssignedCaseworkerPersonId = survivingPersonId;
            changed = true;
        }

        return changed;
    }

    private bool CanReviewInCurrentState(AssistanceCaseStatus decisionStatus)
    {
        var validTarget =
            decisionStatus
            is AssistanceCaseStatus.UnderReview
                or AssistanceCaseStatus.Approved
                or AssistanceCaseStatus.Denied;
        var validCurrent =
            Status is AssistanceCaseStatus.Submitted or AssistanceCaseStatus.UnderReview;
        return validTarget && validCurrent;
    }

    private Result ApproveAward(
        decimal candidateAmount,
        bool requiresVerifiedDocs,
        decimal maxAwardPerCase,
        decimal remainingProgramBudget
    )
    {
        if (requiresVerifiedDocs && !_documents.Exists(static d => d.IsVerified))
        {
            return FinanceErrors.AssistanceDocumentationRequired;
        }

        var validMoney = Money.CreatePositive(candidateAmount, CurrencyCode);
        if (validMoney.IsFailure)
        {
            return validMoney;
        }

        if (
            validMoney.Value.Amount > maxAwardPerCase
            || validMoney.Value.Amount > remainingProgramBudget
        )
        {
            return FinanceErrors.AssistanceBudgetExceeded;
        }

        ApprovedAmount = validMoney.Value.Amount;
        Status = AssistanceCaseStatus.Approved;
        return Result.Success();
    }

    private void ApplyAttributes(IReadOnlyDictionary<string, string?>? attributes)
    {
        if (attributes is null)
        {
            return;
        }

        _attributeValues.Clear();
        foreach (var (key, value) in attributes)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _attributeValues[key.Trim()] = value;
            }
        }
    }

    private static Guid? NormalizeGuid(Guid? value)
    {
        return value is { } guid && guid != Guid.Empty ? guid : null;
    }

    private static string? TrimOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
