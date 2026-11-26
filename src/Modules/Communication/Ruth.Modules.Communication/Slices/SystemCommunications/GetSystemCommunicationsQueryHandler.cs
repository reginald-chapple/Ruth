using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.SystemCommunications;

internal sealed class GetSystemCommunicationsQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetSystemCommunicationsQuery, IReadOnlyList<SystemCommunicationResponse>>
{
    public async Task<Result<IReadOnlyList<SystemCommunicationResponse>>> HandleAsync(
        GetSystemCommunicationsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var dbQuery = dbContext.SystemCommunications.AsNoTracking();
        if (query.Category.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.Category == query.Category.Value);
        }

        if (query.ActiveOnly)
        {
            dbQuery = dbQuery.Where(static c => c.IsActive);
        }

        var list = await dbQuery
            .OrderBy(static c => c.Category)
            .ThenBy(static c => c.Title)
            .ToListAsync(cancellationToken);

        IReadOnlyList<SystemCommunicationResponse> items =
        [
            .. list.Select(SystemCommunicationMapper.ToResponse),
        ];
        return Result.Success(items);
    }
}
