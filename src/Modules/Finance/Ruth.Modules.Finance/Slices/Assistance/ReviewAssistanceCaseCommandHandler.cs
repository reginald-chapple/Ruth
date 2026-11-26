using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Assistance;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Assistance;

internal sealed class ReviewAssistanceCaseCommandHandler(FinanceDbContext dbContext, IClock clock)
    : ICommandHandler<ReviewAssistanceCaseCommand, AssistanceCaseResponse>
{
    public async Task<Result<AssistanceCaseResponse>> HandleAsync(
        ReviewAssistanceCaseCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var caseId = AssistanceCaseId.From(command.CaseId);
        var entity = await dbContext
            .AssistanceCases.Include(static c => c.Documents)
            .Include(static c => c.Disbursements)
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);
        if (entity is null)
        {
            return FinanceErrors.AssistanceCaseNotFound;
        }

        var program = await dbContext
            .AssistancePrograms.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == entity.ProgramId, cancellationToken);
        if (program is null)
        {
            return FinanceErrors.AssistanceProgramNotFound;
        }

        var decision = entity.ApplyReviewDecision(
            command.DecisionStatus,
            command.ApprovedAmount,
            command.AssignedCaseworkerPersonId,
            command.DecisionNotes,
            clock.UtcNow,
            program.RequiresDocumentation,
            program.MaxAwardPerCase,
            program.RemainingBudgetAmount
        );
        if (decision.IsFailure)
        {
            return Result.Failure<AssistanceCaseResponse>(decision.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return AssistanceMapper.ToResponse(entity, program.Name, program.Category);
    }
}
