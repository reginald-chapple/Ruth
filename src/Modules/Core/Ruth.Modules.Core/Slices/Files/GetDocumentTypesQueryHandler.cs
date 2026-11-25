using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class GetDocumentTypesQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetDocumentTypesQuery, IReadOnlyList<DocumentTypeResponse>>
{
    public async Task<Result<IReadOnlyList<DocumentTypeResponse>>> HandleAsync(
        GetDocumentTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext.DocumentTypes.AsNoTracking().AsQueryable();
        if (query.EntityTypeId.HasValue)
        {
            var etId = EntityTypeDescriptorId.From(query.EntityTypeId.Value);
            baseQuery = baseQuery.Where(d => d.EntityTypeId == etId);
        }

        var items = await baseQuery
            .OrderBy(static d => d.Order)
            .ThenBy(static d => d.Name)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<DocumentTypeResponse>>([
            .. items.Select(FileMapper.ToResponse),
        ]);
    }
}
