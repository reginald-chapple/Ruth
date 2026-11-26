namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Snapshot of HTTP request attributes used to evaluate RequestFilter rules and segments.</summary>
public sealed record RequestEvaluationContext(
    string? Host = null,
    string? Path = null,
    string? ClientIpAddress = null,
    string? UserAgent = null,
    IReadOnlyDictionary<string, string>? QueryParameters = null,
    IReadOnlyDictionary<string, string>? Cookies = null,
    IReadOnlyDictionary<string, string>? Headers = null,
    DateTimeOffset? EvaluatedAtUtc = null
);
