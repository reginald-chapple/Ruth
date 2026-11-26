using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;

namespace Ruth.Providers.WalletPasses.Apple;

internal static class ApplePassJsonAndPngAssets
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly byte[] _minimalPngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVQI12NgAAIAAAUAAeImBZsAAAAASUVORK5CYII="
    );

    public static byte[] GetIconPng()
    {
        return _minimalPngBytes;
    }

    public static byte[] BuildPassJsonBytes(WalletPassRenderSpecDto spec, string teamIdentifier)
    {
        var styleSection = BuildStyleSection(spec);
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["formatVersion"] = 1,
            ["passTypeIdentifier"] = spec.PassTypeIdentifier,
            ["teamIdentifier"] = teamIdentifier,
            ["serialNumber"] = spec.SerialNumber,
            ["organizationName"] = spec.OrganizationName,
            ["description"] = spec.Description,
            ["logoText"] = spec.LogoText ?? spec.OrganizationName,
            ["backgroundColor"] = HexToPassKitRgb(spec.BackgroundColorHex),
            ["foregroundColor"] = HexToPassKitRgb(spec.ForegroundColorHex),
            ["labelColor"] = HexToPassKitRgb(spec.LabelColorHex),
            ["authenticationToken"] = spec.AppleAuthenticationToken,
            ["webServiceURL"] = string.IsNullOrWhiteSpace(spec.WebServiceBaseUrl)
                ? null
                : $"{spec.WebServiceBaseUrl.TrimEnd('/')}/api/checkin/wallet",
            ["expirationDate"] = spec
                .ExpiresAtUtc?.ToUniversalTime()
                .ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            ["barcode"] = BuildQrBarcode(spec.ScanToken),
            ["barcodes"] = new[] { BuildQrBarcode(spec.ScanToken) },
            [spec.CardStyle == WalletPassCardStyle.EventTicket ? "eventTicket" : "generic"] =
                styleSection,
        };

        return JsonSerializer.SerializeToUtf8Bytes(payload, _jsonOptions);
    }

    private static object BuildQrBarcode(string scanToken)
    {
        return new
        {
            format = "PKBarcodeFormatQR",
            message = scanToken,
            messageEncoding = "iso-8859-1",
            altText = scanToken,
        };
    }

    private static object BuildStyleSection(WalletPassRenderSpecDto spec)
    {
        var auxSummary = spec.DynamicFields.NextShiftOrBookingSummary
            ?? spec.DynamicFields.EventTicketSummary;
        var auxiliary = string.IsNullOrWhiteSpace(auxSummary)
            ? Array.Empty<object>()
            :
            [
                new
                {
                    key = "next",
                    label = "UPCOMING",
                    value = auxSummary,
                },
            ];

        object[] back =
        [
            .. (spec.DynamicFields.CustomFields ?? []).Select(static f =>
                (object)
                    new
                    {
                        key = f.Key,
                        label = f.Label,
                        value = f.Value,
                    }
            ),
        ];

        return new
        {
            primaryFields = new[]
            {
                new
                {
                    key = "holder",
                    label = "MEMBER",
                    value = spec.DynamicFields.HolderDisplayName ?? "Community Member",
                },
            },
            secondaryFields = BuildSecondaryFields(spec),
            auxiliaryFields = auxiliary,
            backFields = back,
        };
    }

    private static List<object> BuildSecondaryFields(WalletPassRenderSpecDto spec)
    {
        var secondary = new List<object>
        {
            new
            {
                key = "tier",
                label = "TIER",
                value = spec.DynamicFields.MembershipTier ?? "Active",
            },
        };
        if (spec.DynamicFields.PunchOrCreditBalance.HasValue)
        {
            secondary.Add(
                new
                {
                    key = "balance",
                    label = "BALANCE",
                    value = spec.DynamicFields.PunchOrCreditBalance.Value.ToString(
                        CultureInfo.InvariantCulture
                    ),
                }
            );
        }

        return secondary;
    }

    private static string HexToPassKitRgb(string hex)
    {
        var clean = hex.TrimStart('#');
        if (clean.Length != 6)
        {
            return "rgb(30, 58, 138)";
        }

        var r = byte.Parse(
            clean.AsSpan(0, 2),
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture
        );
        var g = byte.Parse(
            clean.AsSpan(2, 2),
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture
        );
        var b = byte.Parse(
            clean.AsSpan(4, 2),
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture
        );
        return $"rgb({r}, {g}, {b})";
    }
}
