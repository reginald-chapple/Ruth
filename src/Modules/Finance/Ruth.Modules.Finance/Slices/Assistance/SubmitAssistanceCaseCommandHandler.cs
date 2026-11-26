using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Assistance;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Assistance;

internal sealed class SubmitAssistanceCaseCommandHandler(FinanceDbContext dbContext, IClock clock)
    : ICommandHandler<SubmitAssistanceCaseCommand, AssistanceCaseResponse>
{
    public async Task<Result<AssistanceCaseResponse>> HandleAsync(
        SubmitAssistanceCaseCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var programId = AssistanceProgramId.From(command.ProgramId);
        var program = await dbContext
            .AssistancePrograms.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == programId, cancellationToken);
        if (program is null)
        {
            return FinanceErrors.AssistanceProgramNotFound;
        }

        var nowUtc = clock.UtcNow;
        if (!program.IsOpenOn(DateOnly.FromDateTime(nowUtc.UtcDateTime)))
        {
            return FinanceErrors.AssistanceProgramInvalid;
        }

        var submitted = AssistanceCase.Submit(
            program.Id,
            command.ApplicantPersonId,
            command.RequestedAmount,
            program.CurrencyCode,
            command.Summary,
            nowUtc,
            command.RequestDetails,
            command.HouseholdId,
            command.BranchId ?? program.BranchId,
            command.AssignedCaseworkerPersonId,
            command.Attributes
        );
        if (submitted.IsFailure)
        {
            return Result.Failure<AssistanceCaseResponse>(submitted.Errors);
        }

        _ = dbContext.AssistanceCases.Add(submitted.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return AssistanceMapper.ToResponse(submitted.Value, program.Name, program.Category);
    }
}
