using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class GetBinaryFilesQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetBinaryFilesQuery, IReadOnlyList<BinaryFileResponse>>
{
    public async Task<Result<IReadOnlyList<BinaryFileResponse>>> HandleAsync(
        GetBinaryFilesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext.BinaryFiles.AsNoTracking().AsQueryable();
        if (query.BinaryFileTypeId.HasValue)
        {
            var typeId = BinaryFileTypeId.From(query.BinaryFileTypeId.Value);
            baseQuery = baseQuery.Where(f => f.BinaryFileTypeId == typeId);
        }

        if (query.IsTemporary.HasValue)
        {
            var isTemp = query.IsTemporary.Value;
            baseQuery = baseQuery.Where(f => f.IsTemporary == isTemp);
        }

        var files = await baseQuery
            .OrderByDescending(static f => f.CreatedOnUtc)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<BinaryFileResponse>>([
            .. files.Select(FileMapper.ToResponse),
        ]);
    }
}
