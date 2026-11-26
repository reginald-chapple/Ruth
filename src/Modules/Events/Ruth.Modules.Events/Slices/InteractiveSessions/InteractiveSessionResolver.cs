using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Domain.InteractiveSessions;
using Ruth.Modules.Events.Infrastructure;

namespace Ruth.Modules.Events.Slices.InteractiveSessions;

internal static class InteractiveSessionResolver
{
    public static Task<InteractiveSession?> LoadTrackedByIdAsync(
        EventsDbContext dbContext,
        Guid sessionId,
        CancellationToken cancellationToken
    )
    {
        var targetId = InteractiveSessionId.From(sessionId);
        return dbContext
            .InteractiveSessions.Include(static s => s.Actions)
            .Include(static s => s.Occurrences)
            .Include(static s => s.Responses)
            .FirstOrDefaultAsync(s => s.Id == targetId, cancellationToken);
    }

    public static Task<InteractiveSession?> LoadByIdOrJoinCodeAsync(
        EventsDbContext dbContext,
        string sessionIdOrJoinCode,
        bool asNoTracking,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(sessionIdOrJoinCode))
        {
            return Task.FromResult<InteractiveSession?>(null);
        }

        var query = asNoTracking
            ? dbContext.InteractiveSessions.AsNoTracking()
            : dbContext.InteractiveSessions.AsQueryable();

        query = query
            .Include(static s => s.Actions)
            .Include(static s => s.Occurrences)
            .Include(static s => s.Responses);

        if (Guid.TryParse(sessionIdOrJoinCode.Trim(), out var guid))
        {
            var id = InteractiveSessionId.From(guid);
            return query.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        var code = sessionIdOrJoinCode.Trim().ToUpperInvariant();
        return query.FirstOrDefaultAsync(s => s.JoinCode == code, cancellationToken);
    }
}
