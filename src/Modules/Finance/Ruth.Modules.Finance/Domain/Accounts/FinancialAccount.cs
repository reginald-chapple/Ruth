using System.Text.RegularExpressions;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Domain.ValueObjects;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Finance.Domain.Accounts;

/// <summary>Hierarchical financial account aggregate supporting charitable funds, grants, membership dues, fees, and operating revenue.</summary>
public sealed partial class FinancialAccount
    : AggregateRoot<FinancialAccountId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    /// <summary>Canonical entity type key for ACL evaluation and entity registry.</summary>
    public const string EntityTypeKeyName = "Ruth.Finance.FinancialAccount";

    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private FinancialAccount() { }

    private FinancialAccount(FinancialAccountId id)
        : base(id) { }

    public FinancialAccountId? ParentAccountId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string PublicName { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public FinancialAccountKind AccountKind { get; private set; }
    public bool IsTaxDeductible { get; private set; }
    public string DefaultCurrencyCode { get; private set; } = "USD";
    public Guid? BranchId { get; private set; }
    public string? GlCode { get; private set; }
    public string? CostCenterCode { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsPublic { get; private set; } = true;

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

    /// <summary>Creates a validated <see cref="FinancialAccount" /> aggregate.</summary>
    public static Result<FinancialAccount> Create(FinancialAccountSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var account = new FinancialAccount(FinancialAccountId.Create());
        var updated = account.Update(spec);
        return updated.IsFailure ? Result.Failure<FinancialAccount>(updated.Errors) : account;
    }

    /// <summary>Validates and updates mutable fields on this financial account.</summary>
    public Result Update(FinancialAccountSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var validation = ValidateSpec(spec, Id);
        if (validation.IsFailure)
        {
            return validation;
        }

        _ = IsoCurrencyCatalog.TryGetScale(spec.DefaultCurrencyCode, out var currency, out _);

        ParentAccountId =
            spec.ParentAccountId is { } parentGuid && parentGuid != Guid.Empty
                ? FinancialAccountId.From(parentGuid)
                : null;
        Name = spec.Name.Trim();
        PublicName = string.IsNullOrWhiteSpace(spec.PublicName) ? Name : spec.PublicName.Trim();
        Code = spec.Code.Trim().ToUpperInvariant();
        Description = TrimOptional(spec.Description, 500);
        AccountKind = spec.AccountKind;
        IsTaxDeductible = spec.IsTaxDeductible;
        DefaultCurrencyCode = currency;
        BranchId = NormalizeOptionalGuid(spec.BranchId);
        GlCode = TrimOptional(spec.GlCode, 64);
        CostCenterCode = TrimOptional(spec.CostCenterCode, 64);
        StartDate = spec.StartDate;
        EndDate = spec.EndDate;
        DisplayOrder = spec.DisplayOrder;
        IsActive = spec.IsActive;
        IsPublic = spec.IsPublic;
        ApplyAttributes(spec.Attributes);

        return Result.Success();
    }

    /// <summary>Determines whether the account is active and within its optional date window on <paramref name="date" />.</summary>
    public bool IsOpenOn(DateOnly date)
    {
        return IsActive
            && (StartDate is null || date >= StartDate.Value)
            && (EndDate is null || date <= EndDate.Value);
    }

    private void ApplyAttributes(IReadOnlyDictionary<string, string?>? attributes)
    {
        if (attributes is null)
        {
            return;
        }

        _attributeValues.Clear();
        foreach (var pair in attributes)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                _attributeValues[pair.Key.Trim()] = pair.Value;
            }
        }
    }

    private static Result ValidateSpec(FinancialAccountSpec spec, FinancialAccountId selfId)
    {
        var identityResult = ValidateIdentityAndCurrency(spec);
        return identityResult.IsFailure ? identityResult : ValidateWindowAndHierarchy(spec, selfId);
    }

    private static Result ValidateIdentityAndCurrency(FinancialAccountSpec spec)
    {
        return IsInvalidAccountName(spec.Name) ? FinanceErrors.AccountNameInvalid
            : !IsValidAccountCode(spec.Code) ? FinanceErrors.AccountCodeInvalid
            : !IsoCurrencyCatalog.TryGetScale(spec.DefaultCurrencyCode, out _, out _)
                ? FinanceErrors.CurrencyCodeInvalid
            : Result.Success();
    }

    private static bool IsInvalidAccountName(string? name)
    {
        return string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160;
    }

    private static bool IsValidAccountCode(string? code)
    {
        var normalizedCode = code?.Trim().ToUpperInvariant() ?? string.Empty;
        return AccountCodeRegex.IsMatch(normalizedCode);
    }

    private static Result ValidateWindowAndHierarchy(
        FinancialAccountSpec spec,
        FinancialAccountId selfId
    )
    {
        return spec.StartDate is not null
            && spec.EndDate is not null
            && spec.EndDate < spec.StartDate
                ? FinanceErrors.AccountDateRangeInvalid
            : spec.ParentAccountId == selfId.Value ? FinanceErrors.AccountHierarchyCycle
            : Result.Success();
    }

    private static Guid? NormalizeOptionalGuid(Guid? value)
    {
        return value is { } guid && guid != Guid.Empty ? guid : null;
    }

    private static string? TrimOptional(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var trimmed = input.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    [GeneratedRegex(
        @"^[A-Z0-9][A-Z0-9_-]{1,63}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex AccountCodeRegex { get; }
}
