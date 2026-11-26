using System.Text.RegularExpressions;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.ValueObjects;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Finance.Domain.Assistance;

/// <summary>Assistance, scholarship, fee-waiver, or micro-grant program aggregate enforcing budget and award ceilings.</summary>
public sealed partial class AssistanceProgram
    : AggregateRoot<AssistanceProgramId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for ACL evaluation and entity registry.</summary>
    public const string EntityTypeKeyName = "Ruth.Finance.AssistanceProgram";

    private AssistanceProgram() { }

    private AssistanceProgram(AssistanceProgramId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public AssistanceProgramCategory Category { get; private set; }
    public FinancialAccountId FundingAccountId { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal TotalBudgetAmount { get; private set; }
    public decimal DisbursedAmount { get; private set; }
    public decimal RemainingBudgetAmount => Math.Max(0m, TotalBudgetAmount - DisbursedAmount);
    public decimal MaxAwardPerCase { get; private set; }
    public string? Description { get; private set; }
    public bool RequiresDocumentation { get; private set; }
    public Guid? BranchId { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsActive { get; private set; } = true;

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

    /// <summary>Creates a validated <see cref="AssistanceProgram" /> aggregate.</summary>
    public static Result<AssistanceProgram> Create(AssistanceProgramSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var program = new AssistanceProgram(AssistanceProgramId.Create());
        var updated = program.Update(spec);
        return updated.IsFailure ? Result.Failure<AssistanceProgram>(updated.Errors) : program;
    }

    /// <summary>Validates and updates mutable assistance program fields while preserving disbursed budget ceilings.</summary>
    public Result Update(AssistanceProgramSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var validation = ValidateSpec(spec, DisbursedAmount, out var currency, out var scale);
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = spec.Name.Trim();
        Code = spec.Code.Trim().ToUpperInvariant();
        Category = spec.Category;
        FundingAccountId = FinancialAccountId.From(spec.FundingAccountId);
        CurrencyCode = currency;
        TotalBudgetAmount = decimal.Round(
            spec.TotalBudgetAmount,
            scale,
            MidpointRounding.AwayFromZero
        );
        MaxAwardPerCase = decimal.Round(spec.MaxAwardPerCase, scale, MidpointRounding.AwayFromZero);
        Description = TrimOptional(spec.Description, 500);
        RequiresDocumentation = spec.RequiresDocumentation;
        BranchId = spec.BranchId is { } guid && guid != Guid.Empty ? guid : null;
        StartDate = spec.StartDate;
        EndDate = spec.EndDate;
        IsActive = spec.IsActive;

        return Result.Success();
    }

    /// <summary>Determines whether the program is active and within its optional date window on <paramref name="date" />.</summary>
    public bool IsOpenOn(DateOnly date)
    {
        return IsActive
            && (StartDate is null || date >= StartDate.Value)
            && (EndDate is null || date <= EndDate.Value);
    }

    /// <summary>Commits a disbursement against the program budget ceiling.</summary>
    public Result CommitDisbursement(decimal amount)
    {
        var validMoney = Money.CreatePositive(amount, CurrencyCode);
        if (validMoney.IsFailure)
        {
            return validMoney;
        }

        if (
            !IsActive
            || validMoney.Value.Amount > MaxAwardPerCase
            || DisbursedAmount + validMoney.Value.Amount > TotalBudgetAmount
        )
        {
            return FinanceErrors.AssistanceBudgetExceeded;
        }

        DisbursedAmount += validMoney.Value.Amount;
        return Result.Success();
    }

    private static Result ValidateSpec(
        AssistanceProgramSpec spec,
        decimal alreadyDisbursed,
        out string currency,
        out int scale
    )
    {
        currency = "USD";
        scale = 2;

        return !IsValidIdentityAndFunding(spec) ? FinanceErrors.AssistanceProgramInvalid
            : !IsoCurrencyCatalog.TryGetScale(spec.CurrencyCode, out currency, out scale)
                ? FinanceErrors.CurrencyCodeInvalid
            : IsValidBudgetAndDates(spec, alreadyDisbursed) ? Result.Success()
            : FinanceErrors.AssistanceProgramInvalid;
    }

    private static bool IsValidIdentityAndFunding(AssistanceProgramSpec spec)
    {
        var normalizedCode = spec.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(spec.Name)
            && spec.Name.Trim().Length <= 160
            && ProgramCodeRegex.IsMatch(normalizedCode)
            && spec.FundingAccountId != Guid.Empty;
    }

    private static bool IsValidBudgetAndDates(AssistanceProgramSpec spec, decimal alreadyDisbursed)
    {
        var validDates =
            spec.StartDate is null || spec.EndDate is null || spec.EndDate >= spec.StartDate;
        return validDates
            && spec.TotalBudgetAmount > 0m
            && spec.TotalBudgetAmount >= alreadyDisbursed
            && spec.MaxAwardPerCase > 0m
            && spec.MaxAwardPerCase <= spec.TotalBudgetAmount;
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

    [GeneratedRegex(
        @"^[A-Z0-9][A-Z0-9_-]{1,63}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex ProgramCodeRegex { get; }
}
