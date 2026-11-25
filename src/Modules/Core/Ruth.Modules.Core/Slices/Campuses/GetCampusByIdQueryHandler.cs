using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Campuses;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Campuses;

internal sealed class GetCampusByIdQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetCampusByIdQuery, CampusResponse>
{
    public async Task<Result<CampusResponse>> HandleAsync(
        GetCampusByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = CampusId.From(query.CampusId);
        var campus = await dbContext
            .Campuses.AsNoTracking()
            .Include(static c => c.Schedules)
            .Include(static c => c.Topics)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return campus is null
            ? CoreLocationErrors.CampusNotFound
            : Result.Success(CampusMapper.ToResponse(campus));
    }
}
