using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Signals;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Signals;

internal sealed class GetSignalTypesQueryHandler(CrmDbContext dbContext)
    : IQueryHandler<GetSignalTypesQuery, IReadOnlyList<SignalTypeResponse>>
{
    public async Task<Result<IReadOnlyList<SignalTypeResponse>>> HandleAsync(
        GetSignalTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var types = await dbContext
            .SignalTypes.AsNoTracking()
            .OrderBy(static s => s.Order)
            .ThenBy(static s => s.Name)
            .ToListAsync(cancellationToken);

        IReadOnlyList<SignalTypeResponse> result =
        [
            .. types.Select(CreateSignalTypeCommandHandler.ToResponse),
        ];
        return Result.Success(result);
    }
}
