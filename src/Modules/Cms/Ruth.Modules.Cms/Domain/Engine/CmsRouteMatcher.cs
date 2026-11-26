namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>Clean-room route template matcher supporting static segments and {parameter} placeholders.</summary>
public static class CmsRouteMatcher
{
    public static CmsRouteMatchResult Match(string routeTemplate, string incomingPath)
    {
        var normalizedTemplate = CmsTextNormalizer.NormalizeRoute(routeTemplate);
        var normalizedPath = CmsTextNormalizer.NormalizeRoute(incomingPath);

        if (normalizedTemplate.Length == 0 && normalizedPath.Length == 0)
        {
            return new CmsRouteMatchResult(
                IsMatch: true,
                1000,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            );
        }

        var templateSegments = SplitSegments(normalizedTemplate);
        var pathSegments = SplitSegments(normalizedPath);
        if (templateSegments.Length != pathSegments.Length)
        {
            return new CmsRouteMatchResult(
                IsMatch: false,
                0,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            );
        }

        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var specificity = 0;

        for (var i = 0; i < templateSegments.Length; i++)
        {
            var tSeg = templateSegments[i];
            var pSeg = pathSegments[i];

            if (TryExtractParameterName(tSeg, out var paramName))
            {
                parameters[paramName] = Uri.UnescapeDataString(pSeg);
                specificity += 10;
            }
            else if (string.Equals(tSeg, pSeg, StringComparison.OrdinalIgnoreCase))
            {
                specificity += 100;
            }
            else
            {
                return new CmsRouteMatchResult(
                    IsMatch: false,
                    0,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                );
            }
        }

        return new CmsRouteMatchResult(IsMatch: true, specificity, parameters);
    }

    public static IReadOnlyDictionary<string, string> ExtractPageContexts(
        IEnumerable<PageContext> contexts,
        IReadOnlyDictionary<string, string> routeParameters,
        IReadOnlyDictionary<string, string>? queryParameters
    )
    {
        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ctx in contexts)
        {
            if (
                routeParameters.TryGetValue(ctx.IdParameter, out var routeVal)
                && !string.IsNullOrWhiteSpace(routeVal)
            )
            {
                resolved[ctx.EntityKey] = routeVal;
            }
            else if (
                queryParameters is not null
                && queryParameters.TryGetValue(ctx.IdParameter, out var queryVal)
                && !string.IsNullOrWhiteSpace(queryVal)
            )
            {
                resolved[ctx.EntityKey] = queryVal;
            }
        }

        return resolved;
    }

    private static string[] SplitSegments(string route)
    {
        return route.Length == 0
            ? []
            : route.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
    }

    private static bool TryExtractParameterName(string segment, out string parameterName)
    {
        if (segment.Length > 2 && segment[0] == '{' && segment[^1] == '}')
        {
            parameterName = segment[1..^1].Trim();
            return parameterName.Length > 0;
        }

        parameterName = string.Empty;
        return false;
    }
}
