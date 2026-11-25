using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.AttributeMatrices;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.AttributeMatrices;

internal sealed class GetAttributeMatrixTemplateByIdQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetAttributeMatrixTemplateByIdQuery, AttributeMatrixTemplateResponse>
{
    public async Task<Result<AttributeMatrixTemplateResponse>> HandleAsync(
        GetAttributeMatrixTemplateByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var id = AttributeMatrixTemplateId.From(query.TemplateId);
        var entity = await dbContext
            .AttributeMatrixTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return entity is null
            ? CoreErrors.MatrixTemplateNotFound
            : MatrixTemplateMapper.ToResponse(entity);
    }
}
