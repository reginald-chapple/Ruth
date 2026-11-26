using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.SupportRequests;

internal sealed class ExpireSupportRequestsCommandHandler(
    EngagementDbContext dbContext,
    IClock clock
) : ICommandHandler<ExpireSupportRequestsCommand, SupportExpirationCleanupResponse>
{
    public async Task<Result<SupportExpirationCleanupResponse>> HandleAsync(
        ExpireSupportRequestsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var now = clock.UtcNow;
        var candidates = await dbContext
            .SupportRequests.Where(r =>
                r.ModerationState != SupportModerationState.Archived && r.ExpiresAtUtc <= now
            )
            .ToListAsync(cancellationToken);

        var archived = 0;
        foreach (var req in candidates)
        {
            if (req.ArchiveIfExpired(now))
            {
                archived++;
            }
        }

        if (archived > 0)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new SupportExpirationCleanupResponse(archived);
    }
}
