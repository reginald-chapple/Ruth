using System.Text.RegularExpressions;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Domain.PaymentMethods;

/// <summary>Tokenized constituent or organization payment method storing only opaque gateway vault tokens and masked last-4 metadata.</summary>
public sealed partial class SavedPaymentMethod
    : AggregateRoot<SavedPaymentMethodId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private SavedPaymentMethod() { }

    private SavedPaymentMethod(
        SavedPaymentMethodId id,
        Guid personId,
        Guid? householdId,
        FinancialGatewayId financialGatewayId,
        string customerVaultToken,
        PaymentMethodKind methodKind,
        string displayName,
        string? cardBrand,
        string accountNumberMasked,
        int? expiryMonth,
        int? expiryYear,
        bool isDefault
    )
        : base(id)
    {
        PersonId = personId;
        HouseholdId = householdId;
        FinancialGatewayId = financialGatewayId;
        CustomerVaultToken = customerVaultToken;
        MethodKind = methodKind;
        DisplayName = displayName;
        CardBrand = cardBrand;
        AccountNumberMasked = accountNumberMasked;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        IsDefault = isDefault;
        IsActive = true;
    }

    public Guid PersonId { get; private set; }
    public Guid? HouseholdId { get; private set; }
    public FinancialGatewayId FinancialGatewayId { get; private set; }
    public string CustomerVaultToken { get; private set; } = string.Empty;
    public PaymentMethodKind MethodKind { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? CardBrand { get; private set; }
    public string AccountNumberMasked { get; private set; } = string.Empty;
    public int? ExpiryMonth { get; private set; }
    public int? ExpiryYear { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

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

    /// <summary>Creates a validated <see cref="SavedPaymentMethod" /> aggregate, rejecting raw PAN strings.</summary>
    public static Result<SavedPaymentMethod> Create(
        Guid personId,
        FinancialGatewayId financialGatewayId,
        string customerVaultToken,
        PaymentMethodKind methodKind,
        string? displayName,
        string? cardBrand,
        string accountNumberMasked,
        int? expiryMonth,
        int? expiryYear,
        Guid? householdId = null,
        bool isDefault = false
    )
    {
        var baseValidation = ValidateInputs(
            personId,
            financialGatewayId,
            customerVaultToken,
            methodKind,
            accountNumberMasked,
            expiryMonth,
            expiryYear
        );
        if (baseValidation.IsFailure)
        {
            return Result.Failure<SavedPaymentMethod>(baseValidation.Errors);
        }

        var normalizedMask = NormalizeMaskedSuffix(accountNumberMasked);
        var (normalizedBrand, effectiveDisplay) = ResolveBrandAndDisplay(
            methodKind,
            cardBrand,
            displayName,
            normalizedMask
        );
        var (cardMonth, cardYear) = NormalizeCardExpiry(methodKind, expiryMonth, expiryYear);

        return new SavedPaymentMethod(
            SavedPaymentMethodId.Create(),
            personId,
            NormalizeGuid(householdId),
            financialGatewayId,
            customerVaultToken.Trim(),
            methodKind,
            effectiveDisplay,
            normalizedBrand,
            normalizedMask,
            cardMonth,
            cardYear,
            isDefault
        );
    }

    private static (string Brand, string Display) ResolveBrandAndDisplay(
        PaymentMethodKind methodKind,
        string? cardBrand,
        string? displayName,
        string normalizedMask
    )
    {
        var brand = string.IsNullOrWhiteSpace(cardBrand)
            ? DefaultBrandForKind(methodKind)
            : cardBrand.Trim();
        var rawDisplay = string.IsNullOrWhiteSpace(displayName)
            ? $"{brand} {normalizedMask}"
            : displayName.Trim();
        var clamped = rawDisplay.Length <= 120 ? rawDisplay : rawDisplay[..120];
        return (brand, clamped);
    }

    private static string DefaultBrandForKind(PaymentMethodKind kind)
    {
        return kind == PaymentMethodKind.AchBankAccount ? "ACH" : "Card";
    }

    private static (int? Month, int? Year) NormalizeCardExpiry(
        PaymentMethodKind kind,
        int? month,
        int? year
    )
    {
        return kind == PaymentMethodKind.CreditCard ? (month, year) : (null, null);
    }

    private static Guid? NormalizeGuid(Guid? id)
    {
        return id is { } guid && guid != Guid.Empty ? guid : null;
    }

    /// <summary>Updates default status flag.</summary>
    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
    }

    /// <summary>Deactivates this vaulted payment method and clears default flag.</summary>
    public void Deactivate()
    {
        IsActive = false;
        IsDefault = false;
    }

    /// <summary>Repoints <see cref="PersonId" /> when duplicate person records are merged.</summary>
    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId != retiredPersonId || survivingPersonId == Guid.Empty)
        {
            return false;
        }

        PersonId = survivingPersonId;
        return true;
    }

    private static Result ValidateInputs(
        Guid personId,
        FinancialGatewayId gatewayId,
        string? vaultToken,
        PaymentMethodKind methodKind,
        string? maskedNumber,
        int? expiryMonth,
        int? expiryYear
    )
    {
        return personId == Guid.Empty ? FinanceErrors.PaymentMethodPersonRequired
            : gatewayId.IsEmpty ? FinanceErrors.GatewayNotFound
            : string.IsNullOrWhiteSpace(vaultToken) || LooksLikeRawPan(vaultToken)
                ? FinanceErrors.PaymentMethodVaultTokenInvalid
            : !IsValidMaskedSuffix(maskedNumber) ? FinanceErrors.PaymentMethodMaskedNumberInvalid
            : methodKind == PaymentMethodKind.CreditCard
            && !HasValidCardExpiry(expiryMonth, expiryYear)
                ? FinanceErrors.PaymentMethodExpiryInvalid
            : Result.Success();
    }

    private static bool HasValidCardExpiry(int? month, int? year)
    {
        return month is >= 1 and <= 12 && year is >= 2020 and <= 2100;
    }

    private static bool LooksLikeRawPan(string candidate)
    {
        return RawPanRunRegex.IsMatch(candidate.Trim());
    }

    private static bool IsValidMaskedSuffix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        return !LooksLikeRawPan(trimmed) && MaskedSuffixRegex.IsMatch(trimmed);
    }

    private static string NormalizeMaskedSuffix(string raw)
    {
        var trimmed = raw.Trim();
        var last4 = trimmed.Length >= 4 ? trimmed[^4..] : trimmed;
        return $"****{last4}";
    }

    [GeneratedRegex(
        @"^\d{13,19}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex RawPanRunRegex { get; }

    [GeneratedRegex(
        @"^(\*{2,15}|x{2,15}|X{2,15})?[0-9A-Za-z]{2,4}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex MaskedSuffixRegex { get; }
}
