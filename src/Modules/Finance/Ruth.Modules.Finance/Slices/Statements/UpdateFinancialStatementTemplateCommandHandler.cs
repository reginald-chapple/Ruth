using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Statements;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Statements;

internal sealed class UpdateFinancialStatementTemplateCommandHandler(FinanceDbContext dbContext)
    : ICommandHandler<UpdateFinancialStatementTemplateCommand, FinancialStatementTemplateResponse>
{
    public async Task<Result<FinancialStatementTemplateResponse>> HandleAsync(
        UpdateFinancialStatementTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = FinancialStatementTemplateId.From(command.TemplateId);
        var template = await dbContext.StatementTemplates.FirstOrDefaultAsync(
            t => t.Id == id,
            cancellationToken
        );
        if (template is null)
        {
            return FinanceErrors.StatementTemplateNotFound;
        }

        var updated = template.Update(command.Spec);
        if (updated.IsFailure)
        {
            return Result.Failure<FinancialStatementTemplateResponse>(updated.Errors);
        }

        if (template.IsDefault)
        {
            await CreateFinancialStatementTemplateCommandHandler.ClearExistingDefaultsAsync(
                dbContext,
                template.StatementKind,
                template.Id,
                cancellationToken
            );
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return StatementMapper.ToResponse(template);
    }
}
