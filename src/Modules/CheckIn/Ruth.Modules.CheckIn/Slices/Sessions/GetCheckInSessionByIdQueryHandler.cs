using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Sessions;

internal sealed class GetCheckInSessionByIdQueryHandler(CheckInDbContext dbContext)
    : IQueryHandler<GetCheckInSessionByIdQuery, CheckInSessionReceiptResponse>
{
    public async Task<Result<CheckInSessionReceiptResponse>> HandleAsync(
        GetCheckInSessionByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = AttendanceCheckInSessionId.From(query.SessionId);
        var session = await dbContext
            .AttendanceCheckInSessions.AsNoTracking()
            .Include(static s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return session is null
            ? Result.Failure<CheckInSessionReceiptResponse>(CheckInErrors.SessionNotFound)
            : Result.Success(CheckInSessionMapper.ToReceipt(session));
    }
}
