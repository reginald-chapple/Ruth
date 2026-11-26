using System.Collections.Frozen;
using System.Text.RegularExpressions;
using Ruth.Modules.CheckIn.Contracts.Labels;

namespace Ruth.Modules.CheckIn.Domain.Labels;

/// <summary>Evaluates declarative {{ Field }} merge tokens for check-in badge and receipt labels.</summary>
public static partial class ZplMergeTokenEvaluator
{
    private static readonly FrozenDictionary<
        string,
        Func<CheckInLabelMergeContextDto, string?>
    > _fieldAccessors = CreateFieldAccessors();

    /// <summary>Checks whether the named context field is non-empty.</summary>
    public static bool HasTruthyValue(string? fieldKey, CheckInLabelMergeContextDto context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(fieldKey))
        {
            return true;
        }

        var val = LookupField(fieldKey.Trim(), context);
        return !string.IsNullOrWhiteSpace(val);
    }

    /// <summary>Substitutes {{ Token }} placeholders in <paramref name="template" /> without ZPL escaping (for visual preview).</summary>
    public static string ResolvePlainText(string? template, CheckInLabelMergeContextDto context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return string.IsNullOrEmpty(template)
            ? string.Empty
            : TokenPattern.Replace(template, match =>
                LookupField(match.Groups["key"].Value.Trim(), context) ?? string.Empty
            );
    }

    /// <summary>Substitutes {{ Token }} placeholders in raw ZPL override templates using safe ^FH_ escaping on every interpolated value.</summary>
    public static string ResolveZplTemplate(string zplTemplate, CheckInLabelMergeContextDto context)
    {
        ArgumentNullException.ThrowIfNull(zplTemplate);
        ArgumentNullException.ThrowIfNull(context);

        return TokenPattern.Replace(zplTemplate, match =>
            ZplTextSanitizer.EscapeForZplField(
                LookupField(match.Groups["key"].Value.Trim(), context)
            )
        );
    }

    private static string? LookupField(string key, CheckInLabelMergeContextDto context)
    {
        return _fieldAccessors.TryGetValue(key, out var accessor) ? accessor(context) : null;
    }

    private static FrozenDictionary<
        string,
        Func<CheckInLabelMergeContextDto, string?>
    > CreateFieldAccessors()
    {
        var map = new Dictionary<string, Func<CheckInLabelMergeContextDto, string?>>(
            StringComparer.OrdinalIgnoreCase
        )
        {
            ["Participant.FullName"] = static c => c.ParticipantFullName,
            ["Participant.NickName"] = static c => c.ParticipantNickName,
            ["Participant.LastName"] = static c => c.ParticipantLastName,
            ["Participant.MembershipTier"] = static c => c.MembershipTier,
            ["Session.SecurityPickupCode"] = static c => c.SecurityPickupCode,
            ["Session.ServiceDate"] = static c => c.ServiceDateText,
            ["Session.CheckedInTime"] = static c => c.CheckedInTimeText,
            ["Entry.AreaName"] = static c => c.AreaName,
            ["Entry.LocationName"] = static c => c.LocationName,
            ["Entry.ScheduleName"] = static c => c.ScheduleName,
            ["Alerts.Medical"] = static c => c.MedicalAllergyAlert,
            ["Alerts.Compliance"] = static c => c.ComplianceAlert,
            ["Alerts.Accommodation"] = static c => c.AccommodationNote,
            ["Alerts.Combined"] = BuildCombinedAlerts,
            ["Guardian.ReceiptSummary"] = static c => c.GuardianReceiptSummary,
            ["Badge.Icons"] = FormatBadgeIcons,
            ["Barcode.Value"] = static c =>
                string.IsNullOrWhiteSpace(c.BarcodeValue) ? c.SecurityPickupCode : c.BarcodeValue,
        };
        return map.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static string? BuildCombinedAlerts(CheckInLabelMergeContextDto c)
    {
        string?[] parts = [c.MedicalAllergyAlert, c.ComplianceAlert, c.AccommodationNote];
        var nonEmpty = parts
            .Where(static p => !string.IsNullOrWhiteSpace(p))
            .Select(static p => p!.Trim());
        var joined = string.Join(" | ", nonEmpty);
        return string.IsNullOrWhiteSpace(joined) ? null : joined;
    }

    private static string? FormatBadgeIcons(CheckInLabelMergeContextDto c)
    {
        var items = new List<string>(4);
        if (!string.IsNullOrWhiteSpace(c.MembershipTier))
        {
            items.Add($"[{c.MembershipTier.Trim().ToUpperInvariant()}]");
        }

        if (c.BadgeIconCodes is not null)
        {
            foreach (var code in c.BadgeIconCodes)
            {
                if (!string.IsNullOrWhiteSpace(code))
                {
                    items.Add($"*{code.Trim().ToUpperInvariant()}*");
                }
            }
        }

        return items.Count == 0 ? null : string.Join(' ', items);
    }

    [GeneratedRegex(
        @"\{\{\s*(?<key>[A-Za-z0-9_.]+)\s*\}\}",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex TokenPattern { get; }
}
