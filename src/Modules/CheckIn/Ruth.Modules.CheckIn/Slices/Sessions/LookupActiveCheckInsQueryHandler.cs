using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.CheckIn.Slices.Sessions;

internal sealed class LookupActiveCheckInsQueryHandler(CheckInDbContext dbContext, IClock clock)
    : IQueryHandler<LookupActiveCheckInsQuery, IReadOnlyList<CheckedInEntryReceiptDto>>
{
    public async Task<Result<IReadOnlyList<CheckedInEntryReceiptDto>>> HandleAsync(
        LookupActiveCheckInsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var serviceDate = query.ServiceDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        var q = dbContext
            .CheckInAttendanceEntries.AsNoTracking()
            .Where(e => e.ServiceDate == serviceDate && e.Status == CheckInEntryStatus.CheckedIn);

        if (query.ConfigurationId is { } cid && cid != Guid.Empty)
        {
            var configId = CheckInConfigurationId.From(cid);
            q = q.Where(e => e.CheckInConfigurationId == configId);
        }

        if (!string.IsNullOrWhiteSpace(query.SecurityPickupCode))
        {
            var code = query.SecurityPickupCode.Trim().ToUpperInvariant();
            q = q.Where(e => e.SecurityPickupCode == code);
        }

        if (query.PersonId is { } pid && pid != Guid.Empty)
        {
            q = q.Where(e => e.PersonId == pid);
        }

        if (query.HouseholdId is { } hid && hid != Guid.Empty)
        {
            var sessionIds = dbContext
                .AttendanceCheckInSessions.AsNoTracking()
                .Where(s => s.ServiceDate == serviceDate && s.HouseholdId == hid)
                .Select(static s => s.Id);
            q = q.Where(e => sessionIds.Contains(e.SessionId));
        }

        var rows = await q.OrderBy(static e => e.PersonFullName)
            .Take(50)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CheckedInEntryReceiptDto>>([
            .. rows.Select(CheckInSessionMapper.ToEntryReceipt),
        ]);
    }
}
