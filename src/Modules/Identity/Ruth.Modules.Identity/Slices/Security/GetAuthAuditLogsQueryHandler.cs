using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class GetAuthAuditLogsQueryHandler(IdentityDbContext dbContext)
    : IQueryHandler<GetAuthAuditLogsQuery, IReadOnlyList<AuthAuditLogResponse>>
{
    public async Task<Result<IReadOnlyList<AuthAuditLogResponse>>> HandleAsync(
        GetAuthAuditLogsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var limit = Math.Clamp(query.MaxCount, 1, 500);
        var logsQuery = dbContext.AuthAuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.EntityTypeKey))
        {
            var typeKey = query.EntityTypeKey.Trim();
            logsQuery = logsQuery.Where(l => l.EntityTypeKey == typeKey);
        }

        if (query.EntityId.HasValue)
        {
            logsQuery = logsQuery.Where(l => l.EntityId == query.EntityId.Value);
        }

        var rows = await logsQuery
            .OrderByDescending(static l => l.ChangedOnUtc)
            .Take(limit)
            .Select(static l => new AuthAuditLogResponse(
                l.Id.Value,
                l.EntityTypeKey,
                l.EntityId,
                l.Action,
                l.ChangeType,
                l.ChangedOnUtc,
                l.ChangedByUserId.HasValue ? l.ChangedByUserId.Value.Value : null,
                l.PreDecision,
                l.PostDecision,
                l.PreOrder,
                l.PostOrder,
                l.SpecialRole,
                l.SecurityRoleId.HasValue ? l.SecurityRoleId.Value.Value : null,
                l.UserAccountId.HasValue ? l.UserAccountId.Value.Value : null
            ))
            .ToListAsync(cancellationToken);

        return rows;
    }
}
