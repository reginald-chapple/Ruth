using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Ruth.Providers.RustFsStorage;

internal static class AwsSigV4Signer
{
    private const string Algorithm = "AWS4-HMAC-SHA256";
    private const string Service = "s3";
    private const string UnsignedPayload = "UNSIGNED-PAYLOAD";

    public static void SignRequest(
        HttpRequestMessage request,
        string accessKey,
        string secretKey,
        string region,
        DateTimeOffset utcNow,
        string payloadHashHex = UnsignedPayload
    )
    {
        if (request.RequestUri is null)
        {
            throw new ArgumentException("Request URI is required.", nameof(request));
        }

        var amzDate = utcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var dateStamp = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var hostHeader = FormatHostHeader(request.RequestUri);

        _ = request.Headers.Remove("Host");
        _ = request.Headers.Remove("x-amz-date");
        _ = request.Headers.Remove("x-amz-content-sha256");
        request.Headers.Host = hostHeader;
        request.Headers.Add("x-amz-date", amzDate);
        request.Headers.Add("x-amz-content-sha256", payloadHashHex);

        var canonicalUri = EncodeCanonicalPath(request.RequestUri.AbsolutePath);
        var canonicalQuery = BuildCanonicalQueryString(request.RequestUri.Query);
        var (canonicalHeaders, signedHeaders) = BuildCanonicalAndSignedHeaders(request, hostHeader);

        var canonicalRequest = string.Join(
            '\n',
            request.Method.Method,
            canonicalUri,
            canonicalQuery,
            canonicalHeaders,
            signedHeaders,
            payloadHashHex
        );

        var credentialScope = $"{dateStamp}/{region}/{Service}/aws4_request";
        var stringToSign = string.Join(
            '\n',
            Algorithm,
            amzDate,
            credentialScope,
            Sha256Hex(canonicalRequest)
        );

        var signingKey = DeriveSigningKey(secretKey, dateStamp, region);
        var signature = HmacSha256Hex(signingKey, stringToSign);
        var authorization =
            $"{Algorithm} Credential={accessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
        _ = request.Headers.TryAddWithoutValidation("Authorization", authorization);
    }

    public static Uri CreatePresignedUrl(
        Uri endpointUri,
        string bucketName,
        string objectKey,
        string httpMethod,
        string accessKey,
        string secretKey,
        string region,
        DateTimeOffset utcNow,
        int expiresInSeconds,
        int maxAllowedSeconds = RustFsStorageOptions.DefaultMaxPresignedLifetimeSeconds,
        IReadOnlyDictionary<string, string>? extraQueryParameters = null
    )
    {
        var boundedMax = Math.Clamp(maxAllowedSeconds, 60, 3_600);
        var boundedExpires = Math.Clamp(expiresInSeconds, 1, boundedMax);
        var amzDate = utcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var dateStamp = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var credentialScope = $"{dateStamp}/{region}/{Service}/aws4_request";
        var hostHeader = FormatHostHeader(endpointUri);
        var normalizedKey = objectKey.TrimStart('/');
        var canonicalUri = EncodeCanonicalPath($"/{bucketName}/{normalizedKey}");

        var queryPairs = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["X-Amz-Algorithm"] = Algorithm,
            ["X-Amz-Credential"] = $"{accessKey}/{credentialScope}",
            ["X-Amz-Date"] = amzDate,
            ["X-Amz-Expires"] = boundedExpires.ToString(CultureInfo.InvariantCulture),
            ["X-Amz-SignedHeaders"] = "host",
        };

        if (extraQueryParameters is not null)
        {
            foreach (var kv in extraQueryParameters)
            {
                if (!string.IsNullOrWhiteSpace(kv.Key))
                {
                    queryPairs[kv.Key] = kv.Value;
                }
            }
        }

        var canonicalQuery = string.Join(
            '&',
            queryPairs.Select(static kv => $"{AwsEncode(kv.Key)}={AwsEncode(kv.Value)}")
        );

        var canonicalRequest = string.Join(
            '\n',
            httpMethod.ToUpperInvariant(),
            canonicalUri,
            canonicalQuery,
            $"host:{hostHeader}\n",
            "host",
            UnsignedPayload
        );

        var stringToSign = string.Join(
            '\n',
            Algorithm,
            amzDate,
            credentialScope,
            Sha256Hex(canonicalRequest)
        );

        var signingKey = DeriveSigningKey(secretKey, dateStamp, region);
        var signature = HmacSha256Hex(signingKey, stringToSign);
        var baseAuthority = endpointUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        return new Uri(
            $"{baseAuthority}{canonicalUri}?{canonicalQuery}&X-Amz-Signature={signature}"
        );
    }

    private static (string CanonicalHeaders, string SignedHeaders) BuildCanonicalAndSignedHeaders(
        HttpRequestMessage request,
        string hostHeader
    )
    {
        var headers = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["host"] = hostHeader,
        };

        foreach (var header in request.Headers)
        {
            var lower = header.Key.ToLowerInvariant();
            if (lower.StartsWith("x-amz-", StringComparison.Ordinal))
            {
                headers[lower] = string.Join(',', header.Value.Select(static v => v.Trim()));
            }
        }

        var canonicalSb = new StringBuilder(128);
        foreach (var kv in headers)
        {
            _ = canonicalSb.Append(kv.Key).Append(':').Append(kv.Value).Append('\n');
        }

        return (canonicalSb.ToString(), string.Join(';', headers.Keys));
    }

    private static string FormatHostHeader(Uri uri)
    {
        return uri.IsDefaultPort
            ? uri.Host
            : string.Create(CultureInfo.InvariantCulture, $"{uri.Host}:{uri.Port}");
    }

    private static string EncodeCanonicalPath(string absolutePath)
    {
        var segments = absolutePath.Split('/');
        return string.Join('/', segments.Select(AwsEncode));
    }

    private static string BuildCanonicalQueryString(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        var trimmed = query.TrimStart('?');
        var pairs = new List<(string Key, string Value)>();
        foreach (var part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=', StringComparison.Ordinal);
            var key = eq >= 0 ? Uri.UnescapeDataString(part[..eq]) : Uri.UnescapeDataString(part);
            var val = eq >= 0 ? Uri.UnescapeDataString(part[(eq + 1)..]) : string.Empty;
            pairs.Add((AwsEncode(key), AwsEncode(val)));
        }

        return string.Join(
            '&',
            pairs
                .OrderBy(static p => p.Key, StringComparer.Ordinal)
                .ThenBy(static p => p.Value, StringComparer.Ordinal)
                .Select(static p => $"{p.Key}={p.Value}")
        );
    }

    private static string AwsEncode(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            var ch = (char)b;
            _ = ch
                is (>= 'A' and <= 'Z')
                    or (>= 'a' and <= 'z')
                    or (>= '0' and <= '9')
                    or '-'
                    or '_'
                    or '.'
                    or '~'
                ? sb.Append(ch)
                : sb.Append(CultureInfo.InvariantCulture, $"%{b:X2}");
        }

        return sb.ToString();
    }

    private static byte[] DeriveSigningKey(string secretKey, string dateStamp, string region)
    {
        var kDate = HmacSha256(Encoding.UTF8.GetBytes($"AWS4{secretKey}"), dateStamp);
        var kRegion = HmacSha256(kDate, region);
        var kService = HmacSha256(kRegion, Service);
        return HmacSha256(kService, "aws4_request");
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string HmacSha256Hex(byte[] key, string data)
    {
        return Convert.ToHexStringLower(HmacSha256(key, data));
    }

    private static string Sha256Hex(string data)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(data)));
    }
}
