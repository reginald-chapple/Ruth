using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Sync;

/// <summary>Lists GroupSync rules for a specific group or across all groups.</summary>
public sealed record GetGroupSyncRulesQuery(Guid? GroupId = null)
    : IQuery<IReadOnlyList<GroupSyncRuleResponse>>;
