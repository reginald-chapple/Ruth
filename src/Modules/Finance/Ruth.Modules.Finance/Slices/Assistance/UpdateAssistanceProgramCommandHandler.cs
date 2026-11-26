using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Assistance;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Assistance;

internal sealed class UpdateAssistanceProgramCommandHandler(FinanceDbContext dbContext)
    : ICommandHandler<UpdateAssistanceProgramCommand, AssistanceProgramResponse>
{
    public async Task<Result<AssistanceProgramResponse>> HandleAsync(
        UpdateAssistanceProgramCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = AssistanceProgramId.From(command.ProgramId);
        var program = await dbContext.AssistancePrograms.FirstOrDefaultAsync(
            p => p.Id == id,
            cancellationToken
        );
        if (program is null)
        {
            return FinanceErrors.AssistanceProgramNotFound;
        }

        var fundingId = FinancialAccountId.From(command.Spec.FundingAccountId);
        var accountExists = await dbContext
            .FinancialAccounts.AsNoTracking()
            .AnyAsync(a => a.Id == fundingId, cancellationToken);
        if (!accountExists)
        {
            return FinanceErrors.AccountNotFound;
        }

        var normalizedCode = command.Spec.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var duplicateCode = await dbContext
            .AssistancePrograms.AsNoTracking()
            .AnyAsync(p => p.Code == normalizedCode && p.Id != id, cancellationToken);
        if (duplicateCode)
        {
            return FinanceErrors.AssistanceProgramCodeDuplicate;
        }

        var updated = program.Update(command.Spec);
        if (updated.IsFailure)
        {
            return Result.Failure<AssistanceProgramResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return AssistanceMapper.ToResponse(program);
    }
}
