using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Sync;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Sync;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Sync;

internal sealed class DeleteGroupSyncRuleCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<DeleteGroupSyncRuleCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteGroupSyncRuleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = GroupSyncRuleId.From(command.RuleId);
        var rule = await dbContext.GroupSyncRules.FirstOrDefaultAsync(
            r => r.Id == id,
            cancellationToken
        );
        if (rule is null)
        {
            return GroupsErrors.SyncRuleNotFound;
        }

        _ = dbContext.GroupSyncRules.Remove(rule);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
