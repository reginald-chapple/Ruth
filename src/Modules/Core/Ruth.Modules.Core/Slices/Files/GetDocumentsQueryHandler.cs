using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class GetDocumentsQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetDocumentsQuery, IReadOnlyList<DocumentResponse>>
{
    public async Task<Result<IReadOnlyList<DocumentResponse>>> HandleAsync(
        GetDocumentsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext.Documents.AsNoTracking().AsQueryable();
        if (query.EntityId.HasValue)
        {
            var entityId = query.EntityId.Value;
            baseQuery = baseQuery.Where(d => d.EntityId == entityId);
        }

        if (query.DocumentTypeId.HasValue)
        {
            var typeId = DocumentTypeId.From(query.DocumentTypeId.Value);
            baseQuery = baseQuery.Where(d => d.DocumentTypeId == typeId);
        }

        var items = await baseQuery
            .OrderByDescending(static d => d.CreatedOnUtc)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<DocumentResponse>>([
            .. items.Select(FileMapper.ToResponse),
        ]);
    }
}
