using System.Text;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Normalizes raw phone inputs into E.164 format, clean digits, reversed suffix index, and display format.</summary>
public static class PhoneNumberNormalizer
{
    /// <summary>Default country calling code when none is supplied.</summary>
    public const string DefaultCountryCode = "1";

    /// <summary>Normalizes a phone number and optional country code.</summary>
    public static Result<NormalizedPhoneNumber> Normalize(string? rawNumber, string? rawCountryCode)
    {
        if (string.IsNullOrWhiteSpace(rawNumber))
        {
            return CrmErrors.PhoneNumberRequired;
        }

        var trimmed = rawNumber.Trim();
        var (countryCode, digits) = ExtractCountryAndDigits(trimmed, rawCountryCode);
        if (countryCode.Length is 0 or > 3 || digits.Length is < 7 or > 20)
        {
            return CrmErrors.PhoneNumberInvalid;
        }

        var formatted = FormatDisplay(countryCode, digits);
        var e164 = $"+{countryCode}{digits}";
        var reversed = ReverseDigits(digits);
        return Result.Success(
            new NormalizedPhoneNumber(countryCode, digits, e164, formatted, reversed)
        );
    }

    /// <summary>Strips all non-digit characters from a phone string.</summary>
    public static string ExtractDigits(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (char.IsAsciiDigit(ch))
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    /// <summary>Reverses a digit string for fast trailing-digit index lookups.</summary>
    public static string ReverseDigits(string digits)
    {
        var chars = digits.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    private static (string CountryCode, string Digits) ExtractCountryAndDigits(
        string trimmed,
        string? rawCountryCode
    )
    {
        var explicitCountry = ExtractDigits(rawCountryCode);
        if (trimmed.StartsWith('+'))
        {
            return ParseInternational(trimmed, explicitCountry);
        }

        var digits = ExtractDigits(trimmed);
        var country = explicitCountry.Length > 0 ? explicitCountry : DefaultCountryCode;
        if (
            string.Equals(country, "1", StringComparison.Ordinal)
            && digits.Length == 11
            && digits[0] == '1'
        )
        {
            digits = digits[1..];
        }

        return (country, digits);
    }

    private static (string CountryCode, string Digits) ParseInternational(
        string trimmed,
        string explicitCountry
    )
    {
        var allDigits = ExtractDigits(trimmed);
        if (explicitCountry.Length > 0)
        {
            var local = allDigits.StartsWith(explicitCountry, StringComparison.Ordinal)
                ? allDigits[explicitCountry.Length..]
                : allDigits;
            return (explicitCountry, local);
        }

        var separatorIndex = trimmed.IndexOfAny([' ', '-', '.', '(']);
        if (separatorIndex is > 1 and <= 4)
        {
            var cc = ExtractDigits(trimmed[1..separatorIndex]);
            var rest = ExtractDigits(trimmed[separatorIndex..]);
            if (cc.Length is >= 1 and <= 3 && rest.Length >= 7)
            {
                return (cc, rest);
            }
        }

        if (allDigits.Length == 11 && allDigits[0] == '1')
        {
            return ("1", allDigits[1..]);
        }

        return allDigits.Length > 10
            ? (allDigits[..^10], allDigits[^10..])
            : (DefaultCountryCode, allDigits);
    }

    private static string FormatDisplay(string countryCode, string digits)
    {
        return string.Equals(countryCode, "1", StringComparison.Ordinal) && digits.Length == 10
                ? $"({digits[..3]}) {digits[3..6]}-{digits[6..]}"
            : digits.Length == 7 ? $"{digits[..3]}-{digits[3..]}"
            : $"+{countryCode} {digits}";
    }
}
