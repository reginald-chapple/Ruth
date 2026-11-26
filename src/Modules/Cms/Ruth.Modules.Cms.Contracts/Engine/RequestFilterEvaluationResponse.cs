namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Result of evaluating a RequestFilter against a request context.</summary>
public sealed record RequestFilterEvaluationResponse(
    Guid RequestFilterId,
    string Key,
    bool IsMatch,
    int MatchedRuleCount,
    int TotalRuleCount
);
