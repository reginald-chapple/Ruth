using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Accounts;

internal sealed class CreateFinancialAccountCommandHandler(FinanceDbContext dbContext)
    : ICommandHandler<CreateFinancialAccountCommand, FinancialAccountResponse>
{
    public async Task<Result<FinancialAccountResponse>> HandleAsync(
        CreateFinancialAccountCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Spec is null)
        {
            return FinanceErrors.AccountNameInvalid;
        }

        if (command.Spec.ParentAccountId is { } parentGuid && parentGuid != Guid.Empty)
        {
            var parentId = FinancialAccountId.From(parentGuid);
            var parentExists = await dbContext.FinancialAccounts.AnyAsync(
                a => a.Id == parentId,
                cancellationToken
            );
            if (!parentExists)
            {
                return FinanceErrors.AccountNotFound;
            }
        }

        var created = FinancialAccount.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<FinancialAccountResponse>(created.Errors);
        }

        var duplicateCode = await dbContext.FinancialAccounts.AnyAsync(
            a => a.Code == created.Value.Code,
            cancellationToken
        );
        if (duplicateCode)
        {
            return FinanceErrors.AccountCodeDuplicate;
        }

        _ = dbContext.FinancialAccounts.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return FinancialAccountMapper.ToResponse(created.Value);
    }
}
