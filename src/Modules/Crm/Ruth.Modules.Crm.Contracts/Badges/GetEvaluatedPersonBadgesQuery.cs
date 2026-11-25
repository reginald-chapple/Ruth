using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Badges;

/// <summary>Query to evaluate all active profile badges for a person.</summary>
public sealed record GetEvaluatedPersonBadgesQuery(Guid PersonId)
    : IQuery<IReadOnlyList<EvaluatedPersonBadgeResponse>>;
