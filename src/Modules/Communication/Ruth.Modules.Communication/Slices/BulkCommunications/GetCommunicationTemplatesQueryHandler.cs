using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class GetCommunicationTemplatesQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetCommunicationTemplatesQuery, IReadOnlyList<CommunicationTemplateResponse>>
{
    public async Task<Result<IReadOnlyList<CommunicationTemplateResponse>>> HandleAsync(
        GetCommunicationTemplatesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.CommunicationTemplates.AsNoTracking();
        if (query.Channel.HasValue)
        {
            q = q.Where(t => t.Channel == query.Channel.Value);
        }

        if (query.ActiveOnly)
        {
            q = q.Where(static t => t.IsActive);
        }

        var items = await q.OrderBy(static t => t.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CommunicationTemplateResponse>>([
            .. items.Select(BulkCommunicationMapper.ToResponse),
        ]);
    }
}
