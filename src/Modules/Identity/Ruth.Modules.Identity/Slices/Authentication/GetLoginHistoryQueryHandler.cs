using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class GetLoginHistoryQueryHandler(IdentityDbContext dbContext)
    : IQueryHandler<GetLoginHistoryQuery, IReadOnlyList<LoginHistoryItemResponse>>
{
    public async Task<Result<IReadOnlyList<LoginHistoryItemResponse>>> HandleAsync(
        GetLoginHistoryQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var limit = Math.Clamp(query.MaxCount, 1, 500);
        var entries = dbContext.LoginHistoryEntries.AsNoTracking().AsQueryable();

        if (query.UserAccountId.HasValue && query.UserAccountId.Value != Guid.Empty)
        {
            var userId = UserAccountId.From(query.UserAccountId.Value);
            entries = entries.Where(e => e.UserAccountId == userId);
        }

        var rows = await entries
            .OrderByDescending(static e => e.OccurredOnUtc)
            .Take(limit)
            .Select(static e => new LoginHistoryItemResponse(
                e.Id.Value,
                e.UserAccountId.HasValue ? e.UserAccountId.Value.Value : null,
                e.UserNameAttempted,
                e.AuthMethod,
                e.ProviderKey,
                e.Succeeded,
                e.FailureReason,
                e.IpAddress,
                e.UserAgent,
                e.OccurredOnUtc
            ))
            .ToListAsync(cancellationToken);

        return rows;
    }
}
