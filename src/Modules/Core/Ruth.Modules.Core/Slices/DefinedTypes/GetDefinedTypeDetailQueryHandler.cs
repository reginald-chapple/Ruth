using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.DefinedTypes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.DefinedTypes;

internal sealed class GetDefinedTypeDetailQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetDefinedTypeDetailQuery, DefinedTypeDetailResponse>
{
    public async Task<Result<DefinedTypeDetailResponse>> HandleAsync(
        GetDefinedTypeDetailQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var id = DefinedTypeId.From(query.DefinedTypeId);
        var entity = await dbContext
            .DefinedTypes.AsNoTracking()
            .Include(static dt => dt.Values)
            .FirstOrDefaultAsync(dt => dt.Id == id, cancellationToken);

        return entity is null
            ? CoreErrors.DefinedTypeNotFound
            : DefinedTypeMapper.ToDetailResponse(entity);
    }
}
