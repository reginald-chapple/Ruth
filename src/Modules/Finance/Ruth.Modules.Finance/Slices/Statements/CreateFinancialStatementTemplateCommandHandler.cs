using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Domain.Statements;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Statements;

internal sealed class CreateFinancialStatementTemplateCommandHandler(FinanceDbContext dbContext)
    : ICommandHandler<CreateFinancialStatementTemplateCommand, FinancialStatementTemplateResponse>
{
    public async Task<Result<FinancialStatementTemplateResponse>> HandleAsync(
        CreateFinancialStatementTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = FinancialStatementTemplate.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<FinancialStatementTemplateResponse>(created.Errors);
        }

        if (created.Value.IsDefault)
        {
            await ClearExistingDefaultsAsync(
                dbContext,
                created.Value.StatementKind,
                created.Value.Id,
                cancellationToken
            );
        }

        _ = dbContext.StatementTemplates.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return StatementMapper.ToResponse(created.Value);
    }

    internal static async Task ClearExistingDefaultsAsync(
        FinanceDbContext dbContext,
        FinancialStatementKind kind,
        FinancialStatementTemplateId excludeId,
        CancellationToken cancellationToken
    )
    {
        var existingDefaults = await dbContext
            .StatementTemplates.Where(t =>
                t.StatementKind == kind && t.IsDefault && t.Id != excludeId
            )
            .ToListAsync(cancellationToken);
        foreach (var item in existingDefaults)
        {
            item.ClearDefault();
        }
    }
}
