using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Query to fetch a hydrated mobile page with linked content channel items and adaptive content.</summary>
public sealed record GetHydratedMobilePageQuery(
    string AppSlug,
    string PageKey,
    Guid? PersonId = null,
    RequestEvaluationContext? RequestContext = null
) : IQuery<HydratedMobilePageResponse>;
