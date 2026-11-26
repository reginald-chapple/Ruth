using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class EvaluateRequestFilterQueryHandler(CmsDbContext db, IClock clock)
    : IQueryHandler<EvaluateRequestFilterQuery, RequestFilterEvaluationResponse>
{
    public async Task<Result<RequestFilterEvaluationResponse>> HandleAsync(
        EvaluateRequestFilterQuery query,
        CancellationToken cancellationToken = default
    )
    {
        RequestFilter? filter;
        if (Guid.TryParse(query.FilterIdOrKey, out var parsedGuid))
        {
            var id = RequestFilterId.From(parsedGuid);
            filter = await db
                .RequestFilters.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        }
        else
        {
            var key = CmsTextNormalizer.NormalizeKey(query.FilterIdOrKey);
            filter = await db
                .RequestFilters.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Key == key, cancellationToken);
        }

        return filter is null
            ? Result.Failure<RequestFilterEvaluationResponse>(
                CmsErrors.NotFound("RequestFilter", query.FilterIdOrKey)
            )
            : Result.Success(RequestFilterEvaluator.Evaluate(filter, query.Context, clock.UtcNow));
    }
}
