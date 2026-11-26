using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class WakeDueEngagementFollowUpsCommandHandler(
    EngagementDbContext dbContext,
    IClock clock
) : ICommandHandler<WakeDueEngagementFollowUpsCommand, WakeupFollowUpsResponse>
{
    public async Task<Result<WakeupFollowUpsResponse>> HandleAsync(
        WakeDueEngagementFollowUpsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var due = await dbContext
            .EngagementRequests.Include(static r => r.Activities)
            .Where(r =>
                r.RequestState == EngagementRequestState.FutureFollowUp
                && r.FollowUpDate.HasValue
                && r.FollowUpDate <= today
            )
            .ToListAsync(cancellationToken);

        var woken = 0;
        foreach (var req in due)
        {
            if (req.WakeIfDue(today, now))
            {
                woken++;
            }
        }

        if (woken > 0)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new WakeupFollowUpsResponse(woken);
    }
}
