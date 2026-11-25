using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class GetBinaryFileTypesQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetBinaryFileTypesQuery, IReadOnlyList<BinaryFileTypeResponse>>
{
    public async Task<Result<IReadOnlyList<BinaryFileTypeResponse>>> HandleAsync(
        GetBinaryFileTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext.BinaryFileTypes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToUpperInvariant();
            baseQuery = baseQuery.Where(t => t.NormalizedName.Contains(term));
        }

        var items = await baseQuery.OrderBy(static t => t.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<BinaryFileTypeResponse>>([
            .. items.Select(FileMapper.ToResponse),
        ]);
    }
}
