using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Journeys;

internal sealed class GetJourneyProgramByIdOrSlugQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetJourneyProgramByIdOrSlugQuery, JourneyProgramResponse>
{
    public async Task<Result<JourneyProgramResponse>> HandleAsync(
        GetJourneyProgramByIdOrSlugQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.IdOrSlug))
        {
            return EngagementErrors.JourneyProgramNotFound;
        }

        var token = query.IdOrSlug.Trim();
        var q = dbContext
            .JourneyPrograms.AsNoTracking()
            .Include(static p => p.Statuses)
            .Include(static p => p.MilestoneTypes);

        var program = Guid.TryParse(token, out var parsed)
            ? await q.FirstOrDefaultAsync(
                p => p.Id == JourneyProgramId.From(parsed),
                cancellationToken
            )
            : await q.FirstOrDefaultAsync(
                p => p.Slug == token.ToLowerInvariant(),
                cancellationToken
            );

        return program is null
            ? EngagementErrors.JourneyProgramNotFound
            : JourneyProjectionMapper.ToResponse(program);
    }
}
