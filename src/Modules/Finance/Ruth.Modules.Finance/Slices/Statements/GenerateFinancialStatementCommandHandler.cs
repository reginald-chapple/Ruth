using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Statements;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Statements;

internal sealed class GenerateFinancialStatementCommandHandler(
    FinanceDbContext dbContext,
    StatementGenerationCollaborator collaborator
) : ICommandHandler<GenerateFinancialStatementCommand, RenderedFinancialStatementDocumentResponse>
{
    public async Task<Result<RenderedFinancialStatementDocumentResponse>> HandleAsync(
        GenerateFinancialStatementCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var templateId = FinancialStatementTemplateId.From(command.TemplateId);
        var template = await dbContext
            .StatementTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive, cancellationToken);

        return template is null
            ? FinanceErrors.StatementTemplateNotFound
            : await collaborator.GenerateAndPersistAsync(
                template,
                command.RecipientScope,
                command.TargetPersonId,
                command.TargetHouseholdId,
                command.PeriodStartDate,
                command.PeriodEndDate,
                command.PersistArchiveToStorage,
                batchRunId: null,
                cancellationToken
            );
    }
}
