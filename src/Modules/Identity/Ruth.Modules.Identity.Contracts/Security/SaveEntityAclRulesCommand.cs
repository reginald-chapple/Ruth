using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Atomically replaces ordered ACL rules for an entity action and writes audit log entries.</summary>
public sealed record SaveEntityAclRulesCommand(
    string EntityTypeKey,
    Guid EntityId,
    string Action,
    IReadOnlyList<AclRuleInput> Rules,
    Guid? ChangedByUserId = null
) : ICommand<IReadOnlyList<EntityAclRuleResponse>>;
