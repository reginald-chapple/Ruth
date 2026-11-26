using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Journeys;

internal sealed class GetJourneyCompletionsQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetJourneyCompletionsQuery, IReadOnlyList<JourneyCompletionResponse>>
{
    public async Task<Result<IReadOnlyList<JourneyCompletionResponse>>> HandleAsync(
        GetJourneyCompletionsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext.JourneyCompletions.AsNoTracking().AsQueryable();
        if (query.JourneyProgramId is { } pid && pid != Guid.Empty)
        {
            q = q.Where(c => c.JourneyProgramId == pid);
        }

        if (query.PersonId is { } personId && personId != Guid.Empty)
        {
            q = q.Where(c => c.PersonId == personId);
        }

        var items = await q
            .OrderByDescending(static c => c.CompletedDate)
            .ToListAsync(cancellationToken);
        var names = await dbContext
            .JourneyPrograms.AsNoTracking()
            .ToDictionaryAsync(static p => p.Id.Value, static p => p.Name, cancellationToken);

        return items
            .Select(c => new JourneyCompletionResponse(
                c.Id.Value,
                c.PersonId,
                c.JourneyProgramId,
                names.GetValueOrDefault(c.JourneyProgramId, "Program"),
                c.BranchId,
                c.StartedDate,
                c.CompletedDate
            ))
            .ToArray();
    }
}
