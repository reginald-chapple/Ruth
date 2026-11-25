using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Attributes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Attributes;

internal sealed class GetAttributeDefinitionByIdQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetAttributeDefinitionByIdQuery, AttributeDefinitionResponse>
{
    public async Task<Result<AttributeDefinitionResponse>> HandleAsync(
        GetAttributeDefinitionByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var id = AttributeDefinitionId.From(query.AttributeDefinitionId);
        var entity = await dbContext
            .AttributeDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity is null)
        {
            return CoreErrors.AttributeNotFound;
        }

        string? entityTypeKey = null;
        if (entity.EntityTypeId.HasValue)
        {
            var typeId = entity.EntityTypeId.Value;
            entityTypeKey = await dbContext
                .EntityTypeDescriptors.AsNoTracking()
                .Where(e => e.Id == typeId)
                .Select(static e => e.Key)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return AttributeMapper.ToResponse(entity, entityTypeKey);
    }
}
