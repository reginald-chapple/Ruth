using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Assessments;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Assessments;

internal sealed class GetAssessmentTypesQueryHandler(CrmDbContext dbContext)
    : IQueryHandler<GetAssessmentTypesQuery, IReadOnlyList<AssessmentTypeResponse>>
{
    public async Task<Result<IReadOnlyList<AssessmentTypeResponse>>> HandleAsync(
        GetAssessmentTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = dbContext.AssessmentTypes.AsNoTracking();
        if (query.ActiveOnly)
        {
            q = q.Where(static t => t.IsActive);
        }

        var types = await q.OrderBy(static t => t.Title).ToListAsync(cancellationToken);
        IReadOnlyList<AssessmentTypeResponse> result =
        [
            .. types.Select(CreateAssessmentTypeCommandHandler.ToResponse),
        ];
        return Result.Success(result);
    }
}
