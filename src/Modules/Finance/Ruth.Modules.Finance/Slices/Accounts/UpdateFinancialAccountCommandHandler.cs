using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Accounts;

internal sealed class UpdateFinancialAccountCommandHandler(FinanceDbContext dbContext)
    : ICommandHandler<UpdateFinancialAccountCommand, FinancialAccountResponse>
{
    public async Task<Result<FinancialAccountResponse>> HandleAsync(
        UpdateFinancialAccountCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Spec is null)
        {
            return FinanceErrors.AccountNameInvalid;
        }

        var targetId = FinancialAccountId.From(command.AccountId);
        var account = await dbContext.FinancialAccounts.FirstOrDefaultAsync(
            a => a.Id == targetId,
            cancellationToken
        );
        if (account is null)
        {
            return FinanceErrors.AccountNotFound;
        }

        var hierarchyCheck = await ValidateParentHierarchyAsync(
            targetId,
            command.Spec.ParentAccountId,
            cancellationToken
        );
        if (hierarchyCheck.IsFailure)
        {
            return Result.Failure<FinancialAccountResponse>(hierarchyCheck.Errors);
        }

        var normalizedCode = command.Spec.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var duplicateCode = await dbContext.FinancialAccounts.AnyAsync(
            a => a.Id != targetId && a.Code == normalizedCode,
            cancellationToken
        );
        if (duplicateCode)
        {
            return FinanceErrors.AccountCodeDuplicate;
        }

        var updated = account.Update(command.Spec);
        if (updated.IsFailure)
        {
            return Result.Failure<FinancialAccountResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return FinancialAccountMapper.ToResponse(account);
    }

    private async Task<Result> ValidateParentHierarchyAsync(
        FinancialAccountId selfId,
        Guid? newParentGuid,
        CancellationToken cancellationToken
    )
    {
        if (newParentGuid is null || newParentGuid.Value == Guid.Empty)
        {
            return Result.Success();
        }

        var cursor = FinancialAccountId.From(newParentGuid.Value);
        if (cursor == selfId)
        {
            return FinanceErrors.AccountHierarchyCycle;
        }

        var parentMap = await dbContext
            .FinancialAccounts.AsNoTracking()
            .Select(static a => new { a.Id, a.ParentAccountId })
            .ToDictionaryAsync(static x => x.Id, static x => x.ParentAccountId, cancellationToken);

        if (!parentMap.ContainsKey(cursor))
        {
            return FinanceErrors.AccountNotFound;
        }

        var visited = new HashSet<FinancialAccountId> { selfId };
        FinancialAccountId? current = cursor;
        while (current is { } nodeId)
        {
            if (!visited.Add(nodeId))
            {
                return FinanceErrors.AccountHierarchyCycle;
            }

            current = parentMap.TryGetValue(nodeId, out var ancestor) ? ancestor : null;
        }

        return Result.Success();
    }
}
