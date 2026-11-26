namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Summary of scheduled cadence Touchpoints generated across active portfolios.</summary>
/// <param name="GeneratedTouchpointCount">Number of new pending cadence touchpoints created.</param>
/// <param name="RefreshedContactCount">Number of constituent contacts whose health scores were refreshed.</param>
public sealed record TouchpointCadenceRunResponse(
    int GeneratedTouchpointCount,
    int RefreshedContactCount
);
