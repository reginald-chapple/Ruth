using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Verification;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Verification;

internal sealed class GetPersonViewedHistoryQueryHandler(CrmDbContext dbContext)
    : IQueryHandler<GetPersonViewedHistoryQuery, IReadOnlyList<PersonViewedResponse>>
{
    public async Task<Result<IReadOnlyList<PersonViewedResponse>>> HandleAsync(
        GetPersonViewedHistoryQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var targetId = PersonId.From(query.TargetPersonId);
        var limit = Math.Clamp(query.MaxResults, 1, 200);

        var rows = await dbContext
            .PersonViewedEntries.AsNoTracking()
            .Where(v => v.TargetPersonId == targetId)
            .OrderByDescending(static v => v.ViewedOnUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        IReadOnlyList<PersonViewedResponse> result =
        [
            .. rows.Select(static v => new PersonViewedResponse(
                v.Id.Value,
                v.TargetPersonId.Value,
                v.ViewerPersonId?.Value,
                v.ViewerUserId,
                v.ViewedOnUtc,
                v.IpAddress,
                v.Source
            )),
        ];
        return Result.Success(result);
    }
}
