using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Flows;

internal sealed class GetCommunicationFlowsQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetCommunicationFlowsQuery, IReadOnlyList<CommunicationFlowResponse>>
{
    public async Task<Result<IReadOnlyList<CommunicationFlowResponse>>> HandleAsync(
        GetCommunicationFlowsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.CommunicationFlows.AsNoTracking();
        if (query.Category.HasValue)
        {
            q = q.Where(f => f.Category == query.Category.Value);
        }

        if (query.ActiveOnly)
        {
            q = q.Where(static f => f.IsActive);
        }

        var items = await q.OrderBy(static f => f.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CommunicationFlowResponse>>([
            .. items.Select(CommunicationFlowMapper.ToResponse),
        ]);
    }
}
