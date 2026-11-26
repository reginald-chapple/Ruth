using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Query to resolve the best eligible adaptation of an AdaptiveMessage for a person and request.</summary>
public sealed record ResolveAdaptiveMessageQuery(
    string AdaptiveMessageKey,
    Guid? PersonId = null,
    RequestEvaluationContext? RequestContext = null,
    bool RecordViewInteraction = false
) : IQuery<ResolvedAdaptiveMessageResponse>;
