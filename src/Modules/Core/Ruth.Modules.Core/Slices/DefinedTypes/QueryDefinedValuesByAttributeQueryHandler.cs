using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.DefinedTypes;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.DefinedTypes;

internal sealed class QueryDefinedValuesByAttributeQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<QueryDefinedValuesByAttributeQuery, IReadOnlyList<DefinedValueResponse>>
{
    public async Task<Result<IReadOnlyList<DefinedValueResponse>>> HandleAsync(
        QueryDefinedValuesByAttributeQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var filterJson = JsonSerializer.Serialize(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [query.AttributeKey.Trim()] = query.AttributeValue,
            }
        );

        var matches = await dbContext
            .DefinedValues.FromSqlInterpolated(
                $"SELECT * FROM core.defined_values WHERE defined_type_id = {query.DefinedTypeId} AND attributes @> CAST({filterJson} AS jsonb) ORDER BY \"order\", value"
            )
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        IReadOnlyList<DefinedValueResponse> results =
        [
            .. matches.Select(DefinedTypeMapper.ToValueResponse),
        ];
        return Result.Success(results);
    }
}
