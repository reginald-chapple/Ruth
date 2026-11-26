using System.Security.Cryptography;
using System.Text;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Domain.PaymentMethods;

/// <summary>Keyed HMAC-SHA256 hasher for ABA routing + bank account pairs, preventing plaintext MICR persistence.</summary>
public static class BankAccountIdentifierHasher
{
    /// <summary>Validates routing/account inputs and computes the keyed HMAC-SHA256 hex digest plus masked last-4 labels.</summary>
    public static Result<(
        string HashHex,
        string MaskedRouting,
        string MaskedAccount
    )> ComputeDigest(string? routingNumber, string? accountNumber, ReadOnlySpan<byte> secretKey)
    {
        if (!AbaRoutingChecksumValidator.TryNormalizeAndValidate(routingNumber, out var routing))
        {
            return FinanceErrors.BankRoutingChecksumInvalid;
        }

        if (!TryNormalizeAccountDigits(accountNumber, out var account))
        {
            return FinanceErrors.BankAccountNumberInvalid;
        }

        var canonical = $"{routing}:{account}";
        var bytes = Encoding.UTF8.GetBytes(canonical);
        Span<byte> mac = stackalloc byte[HMACSHA256.HashSizeInBytes];
        _ = HMACSHA256.HashData(secretKey, bytes, mac);

        var hashHex = Convert.ToHexStringLower(mac);
        var maskedRouting = $"*****{routing[^4..]}";
        var maskedAccount = $"****{account[^4..]}";

        return (hashHex, maskedRouting, maskedAccount);
    }

    private static bool TryNormalizeAccountDigits(string? raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        Span<char> digits = stackalloc char[17];
        var len = 0;
        foreach (var ch in raw)
        {
            if (!TryAppendDigitOrSeparator(ch, digits, ref len))
            {
                return false;
            }
        }

        if (len < 4)
        {
            return false;
        }

        normalized = new string(digits[..len]);
        return true;
    }

    private static bool TryAppendDigitOrSeparator(char ch, Span<char> digits, ref int len)
    {
        if (ch is ' ' or '-')
        {
            return true;
        }

        if (!char.IsAsciiDigit(ch) || len == 17)
        {
            return false;
        }

        digits[len++] = ch;
        return true;
    }
}
