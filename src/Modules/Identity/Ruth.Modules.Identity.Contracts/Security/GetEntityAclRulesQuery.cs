using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Retrieves ordered ACL rules for an entity and optional action.</summary>
public sealed record GetEntityAclRulesQuery(
    string EntityTypeKey,
    Guid EntityId,
    string? Action = null
) : IQuery<IReadOnlyList<EntityAclRuleResponse>>;
