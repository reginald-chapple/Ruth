using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Tenancy.Domain;

internal static class EinFormatter
{
    private static readonly HashSet<string> _invalidPrefixes = new(StringComparer.Ordinal)
    {
        "00",
        "07",
        "08",
        "09",
        "17",
        "18",
        "19",
        "28",
        "29",
        "49",
        "69",
        "70",
        "78",
        "79",
        "89",
        "96",
        "97",
    };

    public static Result<string> NormalizeAndValidate(string? rawEin)
    {
        if (string.IsNullOrWhiteSpace(rawEin))
        {
            return TenancyErrors.InvalidEin;
        }

        Span<char> digits = stackalloc char[9];
        var count = 0;

        foreach (var ch in rawEin.AsSpan().Trim())
        {
            if (ch is '-' or ' ')
            {
                continue;
            }

            if (!char.IsAsciiDigit(ch) || count >= 9)
            {
                return TenancyErrors.InvalidEin;
            }

            digits[count++] = ch;
        }

        if (count != 9)
        {
            return TenancyErrors.InvalidEin;
        }

        var prefix = new string(digits[..2]);
        var serial = new string(digits[2..]);
        return
            _invalidPrefixes.Contains(prefix)
            || string.Equals(serial, "0000000", StringComparison.Ordinal)
            ? TenancyErrors.InvalidEin
            : $"{prefix}-{serial}";
    }
}
