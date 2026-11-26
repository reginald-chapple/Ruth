namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>Result of matching an incoming CMS path against a <see cref="PageRoute" /> template.</summary>
public sealed record CmsRouteMatchResult(
    bool IsMatch,
    int SpecificityScore,
    IReadOnlyDictionary<string, string> Parameters
);
