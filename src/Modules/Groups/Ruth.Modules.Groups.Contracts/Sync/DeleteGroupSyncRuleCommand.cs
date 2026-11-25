using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Sync;

/// <summary>Deletes a GroupSync rule.</summary>
public sealed record DeleteGroupSyncRuleCommand(Guid RuleId) : ICommand<bool>;
