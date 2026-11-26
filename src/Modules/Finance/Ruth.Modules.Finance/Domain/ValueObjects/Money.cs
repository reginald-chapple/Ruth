using System.Globalization;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Domain.ValueObjects;

/// <summary>Value object representing an exact monetary amount and validated ISO-4217 currency code.</summary>
public readonly record struct Money
{
    private Money(decimal amount, string currencyCode)
    {
        Amount = amount;
        CurrencyCode = currencyCode;
    }

    /// <summary>Gets the exact decimal amount normalized to the currency's minor-unit scale.</summary>
    public decimal Amount { get; }

    /// <summary>Gets the 3-letter uppercase ISO-4217 currency code.</summary>
    public string CurrencyCode { get; }

    /// <summary>Creates a validated <see cref="Money" /> instance.</summary>
    public static Result<Money> Create(decimal amount, string? currencyCode = "USD")
    {
        if (!IsoCurrencyCatalog.TryGetScale(currencyCode, out var normalized, out var scale))
        {
            return FinanceErrors.CurrencyCodeInvalid;
        }

        if (!IsoCurrencyCatalog.HasExactScale(amount, scale))
        {
            return FinanceErrors.CurrencyScaleInvalid;
        }

        var canonicalAmount = decimal.Round(amount, scale, MidpointRounding.AwayFromZero);
        return new Money(canonicalAmount, normalized);
    }

    /// <summary>Creates a validated strictly positive <see cref="Money" /> instance (Amount greater than zero).</summary>
    public static Result<Money> CreatePositive(decimal amount, string? currencyCode = "USD")
    {
        var created = Create(amount, currencyCode);
        return created.IsFailure ? created
            : created.Value.Amount <= 0m ? FinanceErrors.AmountMustBePositive
            : created;
    }

    /// <summary>Creates a validated non-negative <see cref="Money" /> instance (Amount greater than or equal to zero).</summary>
    public static Result<Money> CreateNonNegative(decimal amount, string? currencyCode = "USD")
    {
        var created = Create(amount, currencyCode);
        return created.IsFailure ? created
            : created.Value.Amount < 0m ? FinanceErrors.AmountMustBeNonNegative
            : created;
    }

    /// <summary>Creates a zero <see cref="Money" /> value for a valid ISO-4217 currency code.</summary>
    public static Result<Money> Zero(string? currencyCode = "USD")
    {
        return Create(0m, currencyCode);
    }

    /// <summary>Adds two <see cref="Money" /> values of matching currency.</summary>
    public Result<Money> Add(Money other)
    {
        return !HasSameCurrency(other)
            ? FinanceErrors.CurrencyMismatch
            : Create(Amount + other.Amount, CurrencyCode);
    }

    /// <summary>Subtracts <paramref name="other" /> from this <see cref="Money" /> value when currencies match.</summary>
    public Result<Money> Subtract(Money other)
    {
        return !HasSameCurrency(other)
            ? FinanceErrors.CurrencyMismatch
            : Create(Amount - other.Amount, CurrencyCode);
    }

    /// <summary>Allocates this amount proportionally across <paramref name="weights" /> without minor-unit penny loss.</summary>
    public Result<IReadOnlyList<Money>> Allocate(ReadOnlySpan<decimal> weights)
    {
        if (!IsoCurrencyCatalog.TryGetScale(CurrencyCode, out _, out var scale))
        {
            return FinanceErrors.CurrencyCodeInvalid;
        }

        var weightSum = SumValidWeights(weights);
        if (weightSum <= 0m)
        {
            return FinanceErrors.AllocationWeightsInvalid;
        }

        var step = GetMinorUnitStep(scale);
        var totalUnits = decimal.ToInt64(
            decimal.Round(Amount / step, 0, MidpointRounding.AwayFromZero)
        );
        var allocations = AllocateMinorUnits(totalUnits, weights, weightSum, step);
        return Result.Success<IReadOnlyList<Money>>(allocations);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Amount} {CurrencyCode}");
    }

    private bool HasSameCurrency(Money other)
    {
        return string.Equals(CurrencyCode, other.CurrencyCode, StringComparison.Ordinal);
    }

    private static decimal SumValidWeights(ReadOnlySpan<decimal> weights)
    {
        if (weights.IsEmpty)
        {
            return -1m;
        }

        var sum = 0m;
        for (var i = 0; i < weights.Length; i++)
        {
            if (weights[i] < 0m)
            {
                return -1m;
            }

            sum += weights[i];
        }

        return sum;
    }

    private static decimal GetMinorUnitStep(int scale)
    {
        return scale switch
        {
            0 => 1m,
            2 => 0.01m,
            3 => 0.001m,
            _ => 0.01m,
        };
    }

    private Money[] AllocateMinorUnits(
        long totalUnits,
        ReadOnlySpan<decimal> weights,
        decimal weightSum,
        decimal step
    )
    {
        var shares = new long[weights.Length];
        long distributed = 0;

        for (var i = 0; i < weights.Length; i++)
        {
            var rawShare = (long)decimal.Truncate(totalUnits * weights[i] / weightSum);
            shares[i] = rawShare;
            distributed += rawShare;
        }

        var remainder = totalUnits - distributed;
        var direction = remainder >= 0 ? 1L : -1L;
        var count = Math.Abs(remainder);
        var cursor = 0;
        while (count > 0)
        {
            if (weights[cursor] > 0m)
            {
                shares[cursor] += direction;
                count--;
            }

            cursor = (cursor + 1) % weights.Length;
        }

        var result = new Money[weights.Length];
        for (var i = 0; i < weights.Length; i++)
        {
            result[i] = new Money(shares[i] * step, CurrencyCode);
        }

        return result;
    }
}
