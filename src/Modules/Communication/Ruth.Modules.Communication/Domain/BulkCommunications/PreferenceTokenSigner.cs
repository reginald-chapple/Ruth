using System.Security.Cryptography;
using System.Text;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Communication.Domain.BulkCommunications;

/// <summary>Stateless HMAC-SHA256 signer and verifier for recipient unsubscribe and Preference Center tokens.</summary>
public static class PreferenceTokenSigner
{
    /// <summary>Mints a URL-safe signed preference token encoding channel, normalized destination, and optional person ID bound to the tenant.</summary>
    public static string Sign(
        CommunicationChannel channel,
        string normalizedDestination,
        Guid? personId,
        string signingSecret,
        TenantId tenantId = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedDestination);
        ArgumentException.ThrowIfNullOrWhiteSpace(signingSecret);

        var payload =
            $"{channel}|{normalizedDestination.Trim()}|{(personId.HasValue && personId.Value != Guid.Empty ? personId.Value.ToString("D") : string.Empty)}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var payloadB64 = Base64UrlEncode(payloadBytes);
        var macBytes = HMACSHA256.HashData(DeriveSigningKey(signingSecret, tenantId), payloadBytes);
        var macB64 = Base64UrlEncode(macBytes);
        return $"{payloadB64}.{macB64}";
    }

    /// <summary>Verifies and decodes a signed preference token in constant time against the expected tenant.</summary>
    public static Result<(
        CommunicationChannel Channel,
        string NormalizedDestination,
        Guid? PersonId
    )> Verify(string? token, string signingSecret, TenantId expectedTenantId = default)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(signingSecret))
        {
            return Result.Failure<(CommunicationChannel, string, Guid?)>(
                BulkCommunicationErrors.PreferenceTokenInvalid
            );
        }

        var dotIndex = token.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex <= 0 || dotIndex >= token.Length - 1)
        {
            return Result.Failure<(CommunicationChannel, string, Guid?)>(
                BulkCommunicationErrors.PreferenceTokenInvalid
            );
        }

        var payloadBytes = TryBase64UrlDecode(token[..dotIndex]);
        var providedMac = TryBase64UrlDecode(token[(dotIndex + 1)..]);
        if (payloadBytes is null || providedMac is null)
        {
            return Result.Failure<(CommunicationChannel, string, Guid?)>(
                BulkCommunicationErrors.PreferenceTokenInvalid
            );
        }

        var expectedMac = HMACSHA256.HashData(
            DeriveSigningKey(signingSecret, expectedTenantId),
            payloadBytes
        );
        return !CryptographicOperations.FixedTimeEquals(expectedMac, providedMac)
            ? Result.Failure<(CommunicationChannel, string, Guid?)>(
                BulkCommunicationErrors.PreferenceTokenInvalid
            )
            : ParsePayload(Encoding.UTF8.GetString(payloadBytes));
    }

    private static byte[] DeriveSigningKey(string signingSecret, TenantId tenantId)
    {
        return tenantId.IsEmpty
            ? Encoding.UTF8.GetBytes(signingSecret)
            : Encoding.UTF8.GetBytes($"{signingSecret}|tenant:{tenantId.Value:D}");
    }

    private static Result<(
        CommunicationChannel Channel,
        string NormalizedDestination,
        Guid? PersonId
    )> ParsePayload(string decoded)
    {
        var parts = decoded.Split('|');
        if (
            parts.Length != 3
            || !Enum.TryParse<CommunicationChannel>(parts[0], ignoreCase: true, out var channel)
            || string.IsNullOrWhiteSpace(parts[1])
        )
        {
            return Result.Failure<(CommunicationChannel, string, Guid?)>(
                BulkCommunicationErrors.PreferenceTokenInvalid
            );
        }

        Guid? personId =
            Guid.TryParse(parts[2], out var parsed) && parsed != Guid.Empty ? parsed : null;
        return (channel, parts[1], personId);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[]? TryBase64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            0 => padded,
            _ => string.Empty,
        };
        if (padded.Length == 0)
        {
            return null;
        }

        var buffer = new byte[padded.Length * 3 / 4];
        return Convert.TryFromBase64String(padded, buffer, out var written)
            ? buffer[..written]
            : null;
    }
}
