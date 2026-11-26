using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Providers.WalletPasses.Google;

/// <summary>Google Wallet provider generating RS256 signed Save-to-Google-Wallet JWT links.</summary>
public sealed class GoogleWalletJwtProvider(
    IOptionsMonitor<GoogleWalletOptions> optionsMonitor,
    IClock clock
) : IWalletPassProvider
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <inheritdoc />
    public string ProviderKey => "google-wallet";

    /// <inheritdoc />
    public WalletPassPlatform Platform => WalletPassPlatform.GoogleWallet;

    /// <inheritdoc />
    public bool IsConfigured => IsValidConfig(optionsMonitor.CurrentValue);

    /// <inheritdoc />
    public Task<Result<WalletPassArtifactResponse>> RenderPassAsync(
        WalletPassRenderSpecDto spec,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        var opts = optionsMonitor.CurrentValue;
        if (!IsValidConfig(opts))
        {
            return Task.FromResult(
                Result.Failure<WalletPassArtifactResponse>(
                    DomainError.Validation(
                        "WalletPass.GoogleNotConfigured",
                        "Google Wallet IssuerId, ServiceAccountEmail, and PrivateKeyPem are not configured."
                    )
                )
            );
        }

        var jwt = MintSaveToWalletJwt(spec, opts, clock.UtcNow);
        var saveUrl = $"https://pay.google.com/gp/v/save/{jwt}";

        return Task.FromResult(
            Result.Success(
                new WalletPassArtifactResponse(
                    ProviderKey,
                    WalletPassPlatform.GoogleWallet,
                    IsSignedProductionPackage: true,
                    "application/jwt",
                    $"ruth-pass-{spec.SerialNumber}.google.jwt",
                    Encoding.UTF8.GetBytes(jwt),
                    saveUrl,
                    WalletPassSvgCardRenderer.RenderSvg(spec)
                )
            )
        );
    }

    internal static string MintSaveToWalletJwt(
        WalletPassRenderSpecDto spec,
        GoogleWalletOptions opts,
        DateTimeOffset nowUtc
    )
    {
        var headerSegment = Base64Url.EncodeToString(
            JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }, _jsonOptions)
        );

        var genericObject = BuildGenericObjectPayload(spec, opts.IssuerId!.Trim());
        var claims = new
        {
            iss = opts.ServiceAccountEmail!.Trim(),
            aud = "google",
            typ = "savetowallet",
            iat = nowUtc.ToUnixTimeSeconds(),
            origins = opts.AllowedOrigins.Count > 0
                ? opts.AllowedOrigins
                : ["https://localhost:5080"],
            payload = new { genericObjects = new[] { genericObject } },
        };
        var payloadSegment = Base64Url.EncodeToString(
            JsonSerializer.SerializeToUtf8Bytes(claims, _jsonOptions)
        );

        var signingInput = $"{headerSegment}.{payloadSegment}";
        using var rsa = RSA.Create();
        rsa.ImportFromPem(opts.PrivateKeyPem);
        var sigBytes = rsa.SignData(
            Encoding.ASCII.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
        return $"{signingInput}.{Base64Url.EncodeToString(sigBytes)}";
    }

    private static object BuildGenericObjectPayload(WalletPassRenderSpecDto spec, string issuerId)
    {
        return new
        {
            id = $"{issuerId}.{spec.SerialNumber}",
            classId = $"{issuerId}.{spec.PassTypeIdentifier.Replace('.', '_')}",
            state = "ACTIVE",
            hexBackgroundColor = spec.BackgroundColorHex,
            cardTitle = new
            {
                defaultValue = new { language = "en-US", value = spec.OrganizationName },
            },
            header = new
            {
                defaultValue = new
                {
                    language = "en-US",
                    value = spec.DynamicFields.HolderDisplayName ?? "Community Member",
                },
            },
            barcode = new
            {
                type = "QR_CODE",
                value = spec.ScanToken,
                alternateText = spec.ScanToken,
            },
            textModulesData = BuildTextModules(spec),
        };
    }

    private static List<object> BuildTextModules(WalletPassRenderSpecDto spec)
    {
        var textModules = new List<object>
        {
            new
            {
                id = "tier",
                header = "TIER",
                body = spec.DynamicFields.MembershipTier ?? "Active",
            },
        };
        if (spec.DynamicFields.PunchOrCreditBalance.HasValue)
        {
            textModules.Add(
                new
                {
                    id = "balance",
                    header = "CREDITS / PUNCHES",
                    body = spec.DynamicFields.PunchOrCreditBalance.Value.ToString(
                        CultureInfo.InvariantCulture
                    ),
                }
            );
        }

        var nextSummary = spec.DynamicFields.NextShiftOrBookingSummary
            ?? spec.DynamicFields.EventTicketSummary;
        if (!string.IsNullOrWhiteSpace(nextSummary))
        {
            textModules.Add(
                new
                {
                    id = "upcoming",
                    header = "UPCOMING",
                    body = nextSummary,
                }
            );
        }

        return textModules;
    }

    private static bool IsValidConfig(GoogleWalletOptions opts)
    {
        return !string.IsNullOrWhiteSpace(opts.IssuerId)
            && !string.IsNullOrWhiteSpace(opts.ServiceAccountEmail)
            && !string.IsNullOrWhiteSpace(opts.PrivateKeyPem);
    }
}
