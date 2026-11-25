using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Attributes;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Attributes;

internal sealed class GetAttributeDefinitionsQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetAttributeDefinitionsQuery, IReadOnlyList<AttributeDefinitionResponse>>
{
    public async Task<Result<IReadOnlyList<AttributeDefinitionResponse>>> HandleAsync(
        GetAttributeDefinitionsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var entityTypes = await dbContext
            .EntityTypeDescriptors.AsNoTracking()
            .ToDictionaryAsync(static e => e.Id, static e => e.Key, cancellationToken);

        var resolvedEntityTypeId = ResolveEntityTypeId(query, entityTypes);
        var source = dbContext.AttributeDefinitions.AsNoTracking().AsQueryable();

        if (query.GlobalOnly)
        {
            source = source.Where(static a => a.EntityTypeId == null);
        }
        else if (resolvedEntityTypeId.HasValue)
        {
            var id = Domain.EntityTypeDescriptorId.From(resolvedEntityTypeId.Value);
            source = source.Where(a => a.EntityTypeId == id);
        }

        if (query.ActiveOnly)
        {
            source = source.Where(static a => a.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.QualifierColumn))
        {
            var col = query.QualifierColumn.Trim();
            source = source.Where(a => a.EntityTypeQualifierColumn == col);
        }

        if (!string.IsNullOrWhiteSpace(query.QualifierValue))
        {
            var val = query.QualifierValue.Trim();
            source = source.Where(a => a.EntityTypeQualifierValue == val);
        }

        var items = await source
            .OrderBy(static a => a.Order)
            .ThenBy(static a => a.Name)
            .ToListAsync(cancellationToken);

        IReadOnlyList<AttributeDefinitionResponse> results =
        [
            .. items.Select(a =>
                AttributeMapper.ToResponse(
                    a,
                    a.EntityTypeId.HasValue
                        ? entityTypes.GetValueOrDefault(a.EntityTypeId.Value)
                        : null
                )
            ),
        ];

        return Result.Success(results);
    }

    private static Guid? ResolveEntityTypeId(
        GetAttributeDefinitionsQuery query,
        Dictionary<Domain.EntityTypeDescriptorId, string> entityTypes
    )
    {
        if (query.EntityTypeId.HasValue && query.EntityTypeId.Value != Guid.Empty)
        {
            return query.EntityTypeId.Value;
        }

        if (string.IsNullOrWhiteSpace(query.EntityTypeKey))
        {
            return null;
        }

        var match = entityTypes.FirstOrDefault(kvp =>
            string.Equals(kvp.Value, query.EntityTypeKey.Trim(), StringComparison.OrdinalIgnoreCase)
        );
        return match.Key != Domain.EntityTypeDescriptorId.Empty ? match.Key.Value : null;
    }
}
