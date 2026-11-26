namespace Ruth.Modules.Finance.Domain.PaymentMethods;

/// <summary>First-principles 9-digit ABA routing transit number checksum validator.</summary>
public static class AbaRoutingChecksumValidator
{
    /// <summary>Validates that <paramref name="routingNumber" /> is 9 ASCII digits satisfying the ABA weighted modulus-10 checksum.</summary>
    public static bool TryNormalizeAndValidate(string? routingNumber, out string normalized)
    {
        normalized = string.Empty;
        Span<char> buffer = stackalloc char[9];
        if (!TryExtractNineDigits(routingNumber, buffer) || !PassesModulusTen(buffer))
        {
            return false;
        }

        normalized = new string(buffer);
        return true;
    }

    private static bool TryExtractNineDigits(string? routingNumber, Span<char> buffer)
    {
        if (string.IsNullOrWhiteSpace(routingNumber))
        {
            return false;
        }

        var count = 0;
        foreach (var ch in routingNumber)
        {
            if (!TryAppendRoutingDigit(ch, buffer, ref count))
            {
                return false;
            }
        }

        return count == 9;
    }

    private static bool TryAppendRoutingDigit(char ch, Span<char> buffer, ref int count)
    {
        if (ch is ' ' or '-')
        {
            return true;
        }

        if (!char.IsAsciiDigit(ch) || count == 9)
        {
            return false;
        }

        buffer[count++] = ch;
        return true;
    }

    private static bool PassesModulusTen(ReadOnlySpan<char> buffer)
    {
        var weightedSum =
            (3 * (buffer[0] - '0' + buffer[3] - '0' + buffer[6] - '0'))
            + (7 * (buffer[1] - '0' + buffer[4] - '0' + buffer[7] - '0'))
            + buffer[2]
            - '0'
            + buffer[5]
            - '0'
            + buffer[8]
            - '0';

        return weightedSum > 0 && weightedSum % 10 == 0;
    }
}
