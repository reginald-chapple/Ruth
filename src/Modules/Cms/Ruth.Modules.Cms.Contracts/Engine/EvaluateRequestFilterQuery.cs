using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Query to evaluate a CMS request filter by key or ID against a request snapshot.</summary>
public sealed record EvaluateRequestFilterQuery(
    string FilterIdOrKey,
    RequestEvaluationContext Context
) : IQuery<RequestFilterEvaluationResponse>;
