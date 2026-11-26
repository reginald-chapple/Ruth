using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Assistance;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Assistance;

internal sealed class CreateAssistanceProgramCommandHandler(FinanceDbContext dbContext)
    : ICommandHandler<CreateAssistanceProgramCommand, AssistanceProgramResponse>
{
    public async Task<Result<AssistanceProgramResponse>> HandleAsync(
        CreateAssistanceProgramCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var fundingId = FinancialAccountId.From(command.Spec.FundingAccountId);
        var accountExists = await dbContext
            .FinancialAccounts.AsNoTracking()
            .AnyAsync(a => a.Id == fundingId, cancellationToken);
        if (!accountExists)
        {
            return FinanceErrors.AccountNotFound;
        }

        var created = AssistanceProgram.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<AssistanceProgramResponse>(created.Errors);
        }

        var duplicateCode = await dbContext
            .AssistancePrograms.AsNoTracking()
            .AnyAsync(p => p.Code == created.Value.Code, cancellationToken);
        if (duplicateCode)
        {
            return FinanceErrors.AssistanceProgramCodeDuplicate;
        }

        _ = dbContext.AssistancePrograms.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return AssistanceMapper.ToResponse(created.Value);
    }
}
