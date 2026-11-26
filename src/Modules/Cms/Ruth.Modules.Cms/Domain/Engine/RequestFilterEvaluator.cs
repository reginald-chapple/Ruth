using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Ruth.Modules.Cms.Contracts.Engine;

namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>Pure domain evaluator for CMS <see cref="RequestFilter" /> rules.</summary>
public static class RequestFilterEvaluator
{
    public static RequestFilterEvaluationResponse Evaluate(
        RequestFilter filter,
        RequestEvaluationContext context,
        DateTimeOffset fallbackNowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        if (!filter.IsActive || filter.Rules.Count == 0)
        {
            return new RequestFilterEvaluationResponse(
                filter.Id.Value,
                filter.Key,
                IsMatch: false,
                0,
                filter.Rules.Count
            );
        }

        var evaluatedAt = context.EvaluatedAtUtc ?? fallbackNowUtc;
        var matched = 0;
        foreach (var rule in filter.Rules)
        {
            if (EvaluateRule(rule, context, evaluatedAt))
            {
                matched++;
            }
        }

        var isMatch =
            filter.FilterMode == RequestFilterMode.AllMatch
                ? matched == filter.Rules.Count
                : matched > 0;

        return new RequestFilterEvaluationResponse(
            filter.Id.Value,
            filter.Key,
            isMatch,
            matched,
            filter.Rules.Count
        );
    }

    public static string ClassifyDeviceType(string? userAgent)
    {
        return string.IsNullOrWhiteSpace(userAgent) ? "Desktop"
            : userAgent.Contains("Roku", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("AppleTV", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("SmartTV", StringComparison.OrdinalIgnoreCase)
                ? "Tv"
            : userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase)
                ? "Tablet"
            : userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
                ? "Mobile"
            : "Desktop";
    }

    private static bool EvaluateRule(
        RequestFilterRuleSpec rule,
        RequestEvaluationContext ctx,
        DateTimeOffset evaluatedAtUtc
    )
    {
        return rule.Kind switch
        {
            RequestFilterRuleKind.IpAddressRange => MatchesIpOrCidr(
                ctx.ClientIpAddress,
                rule.Value
            ),
            RequestFilterRuleKind.TimeOfDay => MatchesTimeOfDay(evaluatedAtUtc, rule.Value),
            RequestFilterRuleKind.DayOfWeek => MatchesDayOfWeek(evaluatedAtUtc, rule.Value),
            RequestFilterRuleKind.QueryString
            or RequestFilterRuleKind.Cookie
            or RequestFilterRuleKind.Header
            or RequestFilterRuleKind.UserAgentDevice
            or RequestFilterRuleKind.HostDomain
            or _ => CompareStringValue(ResolveSignalValue(rule, ctx), rule.Comparison, rule.Value),
        };
    }

    private static string? ResolveSignalValue(
        RequestFilterRuleSpec rule,
        RequestEvaluationContext ctx
    )
    {
        return rule.Kind switch
        {
            RequestFilterRuleKind.QueryString => Lookup(ctx.QueryParameters, rule.Key),
            RequestFilterRuleKind.Cookie => Lookup(ctx.Cookies, rule.Key),
            RequestFilterRuleKind.Header => Lookup(ctx.Headers, rule.Key),
            RequestFilterRuleKind.UserAgentDevice => ClassifyDeviceType(ctx.UserAgent),
            RequestFilterRuleKind.HostDomain => CmsTextNormalizer.NormalizeHost(ctx.Host),
            RequestFilterRuleKind.IpAddressRange
            or RequestFilterRuleKind.TimeOfDay
            or RequestFilterRuleKind.DayOfWeek
            or _ => null,
        };
    }

    private static string? Lookup(IReadOnlyDictionary<string, string>? map, string key)
    {
        if (map is null || string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (map.TryGetValue(key, out var exact))
        {
            return exact;
        }

        foreach (var pair in map)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return null;
    }

    private static bool CompareStringValue(
        string? actual,
        RequestFilterComparison comparison,
        string? expected
    )
    {
        var exp = expected ?? string.Empty;
        return comparison switch
        {
            RequestFilterComparison.Exists => !string.IsNullOrWhiteSpace(actual),
            RequestFilterComparison.NotExists => string.IsNullOrWhiteSpace(actual),
            RequestFilterComparison.Equals => string.Equals(
                actual,
                exp,
                StringComparison.OrdinalIgnoreCase
            ),
            RequestFilterComparison.NotEquals => !string.Equals(
                actual,
                exp,
                StringComparison.OrdinalIgnoreCase
            ),
            RequestFilterComparison.Contains => actual is not null
                && actual.Contains(exp, StringComparison.OrdinalIgnoreCase),
            RequestFilterComparison.StartsWith => actual is not null
                && actual.StartsWith(exp, StringComparison.OrdinalIgnoreCase),
            RequestFilterComparison.InRange or _ => false,
        };
    }

    private static bool MatchesTimeOfDay(DateTimeOffset nowUtc, string? rangeSpec)
    {
        if (string.IsNullOrWhiteSpace(rangeSpec))
        {
            return false;
        }

        var parts = rangeSpec.Split('-', StringSplitOptions.TrimEntries);
        if (
            parts.Length != 2
            || !TimeOnly.TryParse(parts[0], CultureInfo.InvariantCulture, out var start)
            || !TimeOnly.TryParse(parts[1], CultureInfo.InvariantCulture, out var end)
        )
        {
            return false;
        }

        var current = TimeOnly.FromDateTime(nowUtc.UtcDateTime);
        return start <= end
            ? current >= start && current <= end
            : current >= start || current <= end;
    }

    private static bool MatchesDayOfWeek(DateTimeOffset nowUtc, string? expectedDays)
    {
        if (string.IsNullOrWhiteSpace(expectedDays))
        {
            return false;
        }

        var today = nowUtc.DayOfWeek.ToString();
        return expectedDays
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(day => string.Equals(day, today, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesIpOrCidr(string? clientIpRaw, string? cidrOrIp)
    {
        if (
            string.IsNullOrWhiteSpace(clientIpRaw)
            || string.IsNullOrWhiteSpace(cidrOrIp)
            || !IPAddress.TryParse(clientIpRaw.Trim(), out var clientIp)
        )
        {
            return false;
        }

        var parts = cidrOrIp.Trim().Split('/');
        if (!IPAddress.TryParse(parts[0], out var networkIp))
        {
            return false;
        }

        if (parts.Length == 1)
        {
            return clientIp.Equals(networkIp);
        }

        var maxBits = networkIp.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
        return parts.Length == 2
            && int.TryParse(parts[1], CultureInfo.InvariantCulture, out var prefixBits)
            && prefixBits >= 0
            && prefixBits <= maxBits
            && clientIp.AddressFamily == networkIp.AddressFamily
            && ArePrefixBitsEqual(
                clientIp.GetAddressBytes(),
                networkIp.GetAddressBytes(),
                prefixBits
            );
    }

    private static bool ArePrefixBitsEqual(byte[] clientBytes, byte[] networkBytes, int prefixBits)
    {
        var fullBytes = prefixBits / 8;
        var remainingBits = prefixBits % 8;

        for (var i = 0; i < fullBytes; i++)
        {
            if (clientBytes[i] != networkBytes[i])
            {
                return false;
            }
        }

        if (remainingBits == 0)
        {
            return true;
        }

        var mask = (byte)(0xFF << (8 - remainingBits));
        return (clientBytes[fullBytes] & mask) == (networkBytes[fullBytes] & mask);
    }
}
