using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Sync;

/// <summary>Runs GroupSync reconciliation immediately for a single rule or all due rules.</summary>
public sealed record RunGroupSyncCommand(Guid? RuleId = null, bool ForceRun = true)
    : ICommand<IReadOnlyList<GroupSyncExecutionResponse>>;
