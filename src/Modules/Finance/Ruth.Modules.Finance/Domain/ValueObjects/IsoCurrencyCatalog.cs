using System.Collections.Frozen;

namespace Ruth.Modules.Finance.Domain.ValueObjects;

/// <summary>ISO-4217 currency code catalog defining supported currencies and their minor-unit decimal scales.</summary>
public static class IsoCurrencyCatalog
{
    private static readonly FrozenDictionary<string, int> _scalesByCurrency = new Dictionary<
        string,
        int
    >(StringComparer.Ordinal)
    {
        ["USD"] = 2,
        ["CAD"] = 2,
        ["EUR"] = 2,
        ["GBP"] = 2,
        ["AUD"] = 2,
        ["NZD"] = 2,
        ["CHF"] = 2,
        ["MXN"] = 2,
        ["BRL"] = 2,
        ["SGD"] = 2,
        ["HKD"] = 2,
        ["SEK"] = 2,
        ["NOK"] = 2,
        ["DKK"] = 2,
        ["ZAR"] = 2,
        ["INR"] = 2,
        ["PHP"] = 2,
        ["JPY"] = 0,
        ["KRW"] = 0,
        ["ISK"] = 0,
        ["CLP"] = 0,
        ["BHD"] = 3,
        ["KWD"] = 3,
        ["OMR"] = 3,
        ["JOD"] = 3,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>Normalizes and checks whether <paramref name="currencyCode" /> is a supported ISO-4217 currency, returning its decimal scale.</summary>
    public static bool TryGetScale(
        string? currencyCode,
        out string normalizedCode,
        out int decimalScale
    )
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            normalizedCode = string.Empty;
            decimalScale = 0;
            return false;
        }

        normalizedCode = currencyCode.Trim().ToUpperInvariant();
        return _scalesByCurrency.TryGetValue(normalizedCode, out decimalScale);
    }

    /// <summary>Determines whether <paramref name="amount" /> conforms to <paramref name="decimalScale" /> without sub-minor-unit digits.</summary>
    public static bool HasExactScale(decimal amount, int decimalScale)
    {
        return decimal.Round(amount, decimalScale, MidpointRounding.AwayFromZero) == amount;
    }
}
