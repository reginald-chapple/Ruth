using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.AttributeMatrices;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.AttributeMatrices;

internal sealed class GetAttributeMatrixTemplatesQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<
        GetAttributeMatrixTemplatesQuery,
        IReadOnlyList<AttributeMatrixTemplateResponse>
    >
{
    public async Task<Result<IReadOnlyList<AttributeMatrixTemplateResponse>>> HandleAsync(
        GetAttributeMatrixTemplatesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var source = dbContext.AttributeMatrixTemplates.AsNoTracking().AsQueryable();
        if (query.ActiveOnly)
        {
            source = source.Where(static t => t.IsActive);
        }

        var items = await source.OrderBy(static t => t.Name).ToListAsync(cancellationToken);
        IReadOnlyList<AttributeMatrixTemplateResponse> responses =
        [
            .. items.Select(MatrixTemplateMapper.ToResponse),
        ];
        return Result.Success(responses);
    }
}
