using Ruth.Modules.Groups.Contracts.Sync;
using Ruth.Modules.Groups.Domain.Sync;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Sync;

internal sealed class RunGroupSyncCommandHandler(GroupSyncReconciliationService service)
    : ICommandHandler<RunGroupSyncCommand, IReadOnlyList<GroupSyncExecutionResponse>>
{
    public async Task<Result<IReadOnlyList<GroupSyncExecutionResponse>>> HandleAsync(
        RunGroupSyncCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var ruleId = command.RuleId.HasValue
            ? GroupSyncRuleId.From(command.RuleId.Value)
            : (GroupSyncRuleId?)null;
        var results = await service.ReconcileAsync(ruleId, command.ForceRun, cancellationToken);
        return Result.Success(results);
    }
}
