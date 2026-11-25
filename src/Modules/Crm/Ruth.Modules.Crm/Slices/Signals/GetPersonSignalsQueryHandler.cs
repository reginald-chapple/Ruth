using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Signals;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Signals;

internal sealed class GetPersonSignalsQueryHandler(CrmDbContext dbContext, IClock clock)
    : IQueryHandler<GetPersonSignalsQuery, IReadOnlyList<PersonSignalResponse>>
{
    public async Task<Result<IReadOnlyList<PersonSignalResponse>>> HandleAsync(
        GetPersonSignalsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var personId = PersonId.From(query.PersonId);
        var nowUtc = clock.UtcNow;

        var signals = await dbContext
            .PersonSignals.AsNoTracking()
            .Where(s => s.PersonId == personId)
            .ToListAsync(cancellationToken);

        var types = await dbContext
            .SignalTypes.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id, cancellationToken);

        IReadOnlyList<PersonSignalResponse> result =
        [
            .. signals
                .Where(s =>
                    (query.IncludeExpired || s.IsActiveAt(nowUtc))
                    && types.ContainsKey(s.SignalTypeId)
                )
                .Select(s =>
                {
                    var type = types[s.SignalTypeId];
                    return new PersonSignalResponse(
                        s.Id.Value,
                        s.PersonId.Value,
                        type.Id.Value,
                        type.Name,
                        type.SignalColor,
                        type.SignalIcon,
                        type.Order,
                        s.OwnerPersonId.Value,
                        s.Note,
                        s.ExpirationDateUtc,
                        s.IsActiveAt(nowUtc),
                        s.CreatedOnUtc
                    );
                })
                .OrderBy(static s => s.PriorityOrder)
                .ThenBy(static s => s.SignalTypeName, StringComparer.OrdinalIgnoreCase),
        ];

        return Result.Success(result);
    }
}
