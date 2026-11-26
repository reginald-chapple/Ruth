using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.TwoWaySms;

internal sealed class GetSmsPipelinesQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetSmsPipelinesQuery, IReadOnlyList<SmsPipelineResponse>>
{
    public async Task<Result<IReadOnlyList<SmsPipelineResponse>>> HandleAsync(
        GetSmsPipelinesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.SmsPipelines.AsNoTracking().Include(static p => p.Actions).AsQueryable();
        if (query.ActiveOnly)
        {
            q = q.Where(static p => p.IsActive);
        }

        var items = await q.OrderBy(static p => p.PriorityOrder)
            .ThenBy(static p => p.Name)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<SmsPipelineResponse>>([
            .. items.Select(TwoWaySmsMapper.ToResponse),
        ]);
    }
}
