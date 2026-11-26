using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.PhoneNumbers;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.PhoneNumbers;

internal sealed class GetSystemPhoneNumbersQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetSystemPhoneNumbersQuery, IReadOnlyList<SystemPhoneNumberResponse>>
{
    public async Task<Result<IReadOnlyList<SystemPhoneNumberResponse>>> HandleAsync(
        GetSystemPhoneNumbersQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var dbQuery = dbContext.SystemPhoneNumbers.AsNoTracking();
        if (query.ActiveOnly)
        {
            dbQuery = dbQuery.Where(static x => x.IsActive);
        }

        if (query.SmsEnabledOnly)
        {
            dbQuery = dbQuery.Where(static x => x.IsSmsEnabled);
        }

        var list = await dbQuery.OrderBy(static x => x.Name).ToListAsync(cancellationToken);

        IReadOnlyList<SystemPhoneNumberResponse> items =
        [
            .. list.Select(SystemPhoneNumberMapper.ToResponse),
        ];
        return Result.Success(items);
    }
}
