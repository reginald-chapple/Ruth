using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Assessments;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Assessments;

internal sealed class GetPersonAssessmentsQueryHandler(CrmDbContext dbContext, IClock clock)
    : IQueryHandler<GetPersonAssessmentsQuery, IReadOnlyList<AssessmentResponse>>
{
    public async Task<Result<IReadOnlyList<AssessmentResponse>>> HandleAsync(
        GetPersonAssessmentsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var personId = PersonId.From(query.PersonId);
        var nowUtc = clock.UtcNow;

        var assessments = await dbContext
            .Assessments.AsNoTracking()
            .Where(a => a.PersonId == personId)
            .OrderByDescending(static a => a.CompletedOnUtc ?? a.RequestedOnUtc)
            .ToListAsync(cancellationToken);

        var types = await dbContext
            .AssessmentTypes.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id, cancellationToken);

        IReadOnlyList<AssessmentResponse> result =
        [
            .. assessments
                .Where(a => types.ContainsKey(a.AssessmentTypeId))
                .Select(a =>
                    RequestAssessmentCommandHandler.ToResponse(a, types[a.AssessmentTypeId], nowUtc)
                ),
        ];

        return Result.Success(result);
    }
}
