using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Badges;

/// <summary>Query to list all configured badge definitions.</summary>
public sealed record GetBadgeDefinitionsQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<BadgeDefinitionResponse>>;
