using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Labels;

internal sealed class GetCheckInLabelsQueryHandler(CheckInDbContext dbContext)
    : IQueryHandler<GetCheckInLabelsQuery, IReadOnlyList<CheckInLabelResponse>>
{
    public async Task<Result<IReadOnlyList<CheckInLabelResponse>>> HandleAsync(
        GetCheckInLabelsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.CheckInLabels.AsNoTracking();
        if (query.LabelKind.HasValue)
        {
            q = q.Where(l => l.LabelKind == query.LabelKind.Value);
        }

        if (query.ConfigurationId is { } cid && cid != Guid.Empty)
        {
            q = q.Where(l => !l.ConfigurationId.HasValue || l.ConfigurationId == cid);
        }

        if (query.ActiveOnly)
        {
            q = q.Where(static l => l.IsActive);
        }

        var rows = await q.OrderBy(static l => l.PrintOrder)
            .ThenBy(static l => l.Name)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CheckInLabelResponse>>([
            .. rows.Select(CheckInLabelMapper.ToResponse),
        ]);
    }
}
