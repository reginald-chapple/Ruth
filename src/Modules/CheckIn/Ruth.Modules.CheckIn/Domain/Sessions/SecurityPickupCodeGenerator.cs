using System.Buffers;
using System.Security.Cryptography;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Domain.Sessions;

/// <summary>
/// High-throughput, clean-room daily security pickup code generator using unambiguous uppercase characters,
/// leetspeak normalization, and a <see cref="SearchValues{T}" /> profanity/blocked-substring filter.
/// </summary>
public static class SecurityPickupCodeGenerator
{
    /// <summary>Unambiguous uppercase alphabet (omitting I and O to prevent confusion with 1 and 0 on badges).</summary>
    public const string UnambiguousAlphaAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ";

    /// <summary>Numeric digits alphabet.</summary>
    public const string NumericAlphabet = "0123456789";

    private static readonly string[] _builtInBlockedTokens =
    [
        "ASS",
        "BAD",
        "BUM",
        "CNT",
        "CUM",
        "DAM",
        "DIC",
        "DIE",
        "DIK",
        "DUM",
        "FAG",
        "FAT",
        "FCK",
        "FUK",
        "FUC",
        "GAY",
        "GOD",
        "GUN",
        "H8R",
        "HEL",
        "JAP",
        "JEW",
        "KIL",
        "KKK",
        "LSD",
        "NIG",
        "PIS",
        "POO",
        "PRN",
        "PUS",
        "RAP",
        "SEX",
        "SH1",
        "SHT",
        "SLT",
        "SLU",
        "STD",
        "SUK",
        "THC",
        "TIT",
        "VAG",
        "WOP",
        "WTF",
        "XXX",
        "666",
    ];

    private static readonly SearchValues<string> _blockedSubstringSearch = SearchValues.Create(
        _builtInBlockedTokens,
        StringComparison.OrdinalIgnoreCase
    );

    /// <summary>
    /// Generates a collision-free, profanity-filtered daily security pickup code matching the requested
    /// <paramref name="alphaLength" /> and <paramref name="numericLength" />.
    /// </summary>
    public static Result<string> Generate(
        int alphaLength,
        int numericLength,
        IReadOnlySet<string>? alreadyIssuedToday = null,
        IReadOnlyCollection<string>? additionalBlockedSubstrings = null,
        Func<int, int>? randomIndexSelector = null,
        int maxAttempts = 256
    )
    {
        return !AreValidLengths(alphaLength, numericLength)
            ? Result.Failure<string>(CheckInErrors.InvalidSecurityCodeLength)
            : TryGenerateUniqueCode(
                alphaLength,
                numericLength,
                alreadyIssuedToday,
                additionalBlockedSubstrings,
                ResolveRandomSelector(randomIndexSelector),
                Math.Clamp(maxAttempts, 1, 4096)
            );
    }

    /// <summary>Checks whether <paramref name="candidate" /> (or its leetspeak equivalent) contains any blocked substring.</summary>
    public static bool IsSafeCode(
        ReadOnlySpan<char> candidate,
        IReadOnlyCollection<string>? additionalBlockedSubstrings = null
    )
    {
        return candidate.Length > 0
            && !ContainsBlockedToken(candidate, additionalBlockedSubstrings);
    }

    /// <summary>Generates a short uppercase express barcode token for mobile pre-check-in sessions.</summary>
    public static string GenerateMobileExpressToken()
    {
        Span<char> chars = stackalloc char[10];
        "CHK-".AsSpan().CopyTo(chars);
        for (var i = 4; i < chars.Length; i++)
        {
            chars[i] = UnambiguousAlphaAlphabet[
                RandomNumberGenerator.GetInt32(UnambiguousAlphaAlphabet.Length)
            ];
        }

        return new string(chars);
    }

    private static bool AreValidLengths(int alphaLength, int numericLength)
    {
        return IsSingleSegmentValid(alphaLength)
            && IsSingleSegmentValid(numericLength)
            && IsCombinedLengthValid(alphaLength + numericLength);
    }

    private static bool IsSingleSegmentValid(int length)
    {
        return length is >= 0 and <= 6;
    }

    private static bool IsCombinedLengthValid(int totalLength)
    {
        return totalLength is > 0 and <= 8;
    }

    private static Func<int, int> ResolveRandomSelector(Func<int, int>? custom)
    {
        return custom ?? RandomNumberGenerator.GetInt32;
    }

    private static Result<string> TryGenerateUniqueCode(
        int alphaLength,
        int numericLength,
        IReadOnlySet<string>? alreadyIssuedToday,
        IReadOnlyCollection<string>? additionalBlockedSubstrings,
        Func<int, int> selector,
        int maxAttempts
    )
    {
        Span<char> buffer = stackalloc char[alphaLength + numericLength];
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            FillCandidate(buffer, alphaLength, numericLength, selector);
            var candidate = TryAcceptCandidate(
                buffer,
                alreadyIssuedToday,
                additionalBlockedSubstrings
            );
            if (candidate is not null)
            {
                return Result.Success(candidate);
            }
        }

        return Result.Failure<string>(CheckInErrors.SecurityCodePoolExhausted);
    }

    private static string? TryAcceptCandidate(
        ReadOnlySpan<char> buffer,
        IReadOnlySet<string>? alreadyIssuedToday,
        IReadOnlyCollection<string>? additionalBlockedSubstrings
    )
    {
        if (ContainsBlockedToken(buffer, additionalBlockedSubstrings))
        {
            return null;
        }

        var candidate = new string(buffer);
        return alreadyIssuedToday is not null && alreadyIssuedToday.Contains(candidate)
            ? null
            : candidate;
    }

    private static void FillCandidate(
        Span<char> buffer,
        int alphaLength,
        int numericLength,
        Func<int, int> selector
    )
    {
        for (var i = 0; i < alphaLength; i++)
        {
            var idx =
                Math.Abs(selector(UnambiguousAlphaAlphabet.Length))
                % UnambiguousAlphaAlphabet.Length;
            buffer[i] = UnambiguousAlphaAlphabet[idx];
        }

        for (var j = 0; j < numericLength; j++)
        {
            var idx = Math.Abs(selector(NumericAlphabet.Length)) % NumericAlphabet.Length;
            buffer[alphaLength + j] = NumericAlphabet[idx];
        }
    }

    private static bool ContainsBlockedToken(
        ReadOnlySpan<char> raw,
        IReadOnlyCollection<string>? customBlocked
    )
    {
        if (raw.IndexOfAny(_blockedSubstringSearch) >= 0)
        {
            return true;
        }

        Span<char> leetNormalized = stackalloc char[raw.Length];
        for (var i = 0; i < raw.Length; i++)
        {
            leetNormalized[i] = MapLeetCharacter(char.ToUpperInvariant(raw[i]));
        }

        return leetNormalized.IndexOfAny(_blockedSubstringSearch) >= 0
            || (
                customBlocked is not null
                && MatchesCustomBlocked(raw, leetNormalized, customBlocked)
            );
    }

    private static bool MatchesCustomBlocked(
        ReadOnlySpan<char> raw,
        ReadOnlySpan<char> leetNormalized,
        IReadOnlyCollection<string> customBlocked
    )
    {
        foreach (var token in customBlocked)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            var trimmed = token.AsSpan().Trim();
            if (
                raw.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                || leetNormalized.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }
        }

        return false;
    }

    private static char MapLeetCharacter(char upper)
    {
        return upper switch
        {
            '0' => 'O',
            '1' => 'I',
            '3' => 'E',
            '4' => 'A',
            '5' => 'S',
            '7' => 'T',
            '8' => 'B',
            _ => upper,
        };
    }
}
