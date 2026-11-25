using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Assessments;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Assessments;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Assessments;

internal sealed class CompleteAssessmentCommandHandler(CrmDbContext dbContext, IClock clock)
    : ICommandHandler<CompleteAssessmentCommand, AssessmentResponse>
{
    public async Task<Result<AssessmentResponse>> HandleAsync(
        CompleteAssessmentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = AssessmentId.From(command.AssessmentId);
        var assessment = await dbContext.Assessments.FirstOrDefaultAsync(
            a => a.Id == id,
            cancellationToken
        );
        if (assessment is null)
        {
            return CrmErrors.AssessmentNotFound;
        }

        var nowUtc = clock.UtcNow;
        var completed = assessment.Complete(command.ResultDataJson, command.SummaryText, nowUtc);
        if (completed.IsFailure)
        {
            return Result.Failure<AssessmentResponse>(completed.Errors);
        }

        var type = await dbContext
            .AssessmentTypes.AsNoTracking()
            .FirstAsync(t => t.Id == assessment.AssessmentTypeId, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(RequestAssessmentCommandHandler.ToResponse(assessment, type, nowUtc));
    }
}
