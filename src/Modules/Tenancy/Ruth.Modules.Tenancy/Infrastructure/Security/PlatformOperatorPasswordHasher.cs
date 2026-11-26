using System.Security.Cryptography;

namespace Ruth.Modules.Tenancy.Infrastructure.Security;

internal static class PlatformOperatorPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 210_000;

    public static string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        Span<byte> salt = stackalloc byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        Span<byte> derived = stackalloc byte[KeySize];
        Rfc2898DeriveBytes.Pbkdf2(password, salt, derived, Iterations, HashAlgorithmName.SHA512);

        return $"pbkdf2-sha512${Convert.ToBase64String(salt)}${Convert.ToBase64String(derived)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('$');
        if (
            parts.Length != 3
            || !string.Equals(parts[0], "pbkdf2-sha512", StringComparison.Ordinal)
        )
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        Span<byte> actual = stackalloc byte[expected.Length];
        Rfc2898DeriveBytes.Pbkdf2(password, salt, actual, Iterations, HashAlgorithmName.SHA512);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
