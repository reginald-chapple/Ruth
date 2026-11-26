using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class GetRegistrationTemplatesQueryHandler(EventsDbContext dbContext)
    : IQueryHandler<GetRegistrationTemplatesQuery, IReadOnlyList<RegistrationTemplateResponse>>
{
    public async Task<Result<IReadOnlyList<RegistrationTemplateResponse>>> HandleAsync(
        GetRegistrationTemplatesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext
            .RegistrationTemplates.AsNoTracking()
            .Include(static t => t.Forms)
                .ThenInclude(static f => f.Fields)
            .Include(static t => t.Fees)
                .ThenInclude(static fee => fee.Items)
            .Include(static t => t.Discounts)
            .Include(static t => t.GroupPlacements)
            .AsSplitQuery();

        if (query.ActiveOnly)
        {
            q = q.Where(static t => t.IsActive);
        }

        var items = await q.OrderBy(static t => t.Name).ToListAsync(cancellationToken);
        return items.Select(RegistrationMapper.MapTemplate).ToArray();
    }
}
