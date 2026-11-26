using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Journeys;

internal sealed class GetPersonJourneyProgressQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetPersonJourneyProgressQuery, IReadOnlyList<PersonJourneySummaryResponse>>
{
    public async Task<Result<IReadOnlyList<PersonJourneySummaryResponse>>> HandleAsync(
        GetPersonJourneyProgressQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var progQuery = dbContext
            .JourneyPrograms.AsNoTracking()
            .Include(static p => p.Statuses)
            .Include(static p => p.MilestoneTypes)
            .Where(static p => p.IsActive);
        if (query.JourneyProgramId is { } pid && pid != Guid.Empty)
        {
            progQuery = progQuery.Where(p => p.Id == JourneyProgramId.From(pid));
        }

        var programs = await progQuery.OrderBy(static p => p.Name).ToListAsync(cancellationToken);
        var milestones = await dbContext
            .PersonMilestones.AsNoTracking()
            .Where(m => m.PersonId == query.PersonId)
            .OrderBy(static m => m.StartDate)
            .ToListAsync(cancellationToken);
        var completions = await dbContext
            .JourneyCompletions.AsNoTracking()
            .Where(c => c.PersonId == query.PersonId)
            .ToDictionaryAsync(static c => c.JourneyProgramId, cancellationToken);

        return programs
            .Select(p =>
            {
                var pMilestones = milestones
                    .Where(m => m.JourneyProgramId == p.Id.Value)
                    .Select(m => JourneyProjectionMapper.ToResponse(m, p))
                    .ToArray();
                var completedIds = pMilestones
                    .Where(static m => m.IsCompleted)
                    .Select(static m => m.MilestoneTypeId)
                    .ToHashSet();
                var required = p
                    .MilestoneTypes.Where(static m => m.IsActive && m.IsRequiredForProgramCompletion)
                    .ToArray();
                var hasCompletion = completions.TryGetValue(p.Id.Value, out var comp);

                return new PersonJourneySummaryResponse(
                    query.PersonId,
                    p.Id.Value,
                    p.Name,
                    required.Count(m => completedIds.Contains(m.Id.Value)),
                    required.Length,
                    hasCompletion,
                    comp?.CompletedDate,
                    pMilestones
                );
            })
            .ToArray();
    }
}
