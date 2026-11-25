using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Assessments;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Assessments;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Assessments;

internal sealed class RequestAssessmentCommandHandler(CrmDbContext dbContext, IClock clock)
    : ICommandHandler<RequestAssessmentCommand, AssessmentResponse>
{
    public async Task<Result<AssessmentResponse>> HandleAsync(
        RequestAssessmentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var personId = PersonId.From(command.PersonId);
        var typeId = AssessmentTypeId.From(command.AssessmentTypeId);

        var personExists = await dbContext.People.AnyAsync(
            p => p.Id == personId,
            cancellationToken
        );
        if (!personExists)
        {
            return CrmErrors.PersonNotFound;
        }

        var assessmentType = await dbContext
            .AssessmentTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == typeId, cancellationToken);
        if (assessmentType is null)
        {
            return CrmErrors.AssessmentTypeNotFound;
        }

        var lastCompletedOnUtc = await dbContext
            .Assessments.AsNoTracking()
            .Where(a =>
                a.PersonId == personId
                && a.AssessmentTypeId == typeId
                && a.Status == AssessmentStatus.Complete
            )
            .OrderByDescending(static a => a.CompletedOnUtc)
            .Select(static a => a.CompletedOnUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var nowUtc = clock.UtcNow;
        var created = Assessment.Request(
            personId,
            assessmentType,
            command.RequesterPersonId.HasValue
                ? PersonId.From(command.RequesterPersonId.Value)
                : null,
            command.RequestedDueDate,
            lastCompletedOnUtc,
            nowUtc
        );
        if (created.IsFailure)
        {
            return Result.Failure<AssessmentResponse>(created.Errors);
        }

        _ = dbContext.Assessments.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(ToResponse(created.Value, assessmentType, nowUtc));
    }

    internal static AssessmentResponse ToResponse(
        Assessment a,
        AssessmentType type,
        DateTimeOffset nowUtc
    )
    {
        return new AssessmentResponse(
            a.Id.Value,
            a.PersonId.Value,
            type.Id.Value,
            type.Key,
            type.Title,
            a.RequesterPersonId?.Value,
            a.RequestedOnUtc,
            a.RequestedDueDate,
            a.Status,
            a.CompletedOnUtc,
            a.ResultDataJson,
            a.SummaryText,
            a.IsExpiredAt(type.ValidDurationDays, nowUtc)
        );
    }
}
