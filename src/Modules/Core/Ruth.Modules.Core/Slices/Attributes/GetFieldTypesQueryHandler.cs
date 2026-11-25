using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.Modules.Core.Domain.FieldTypes;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Attributes;

internal sealed class GetFieldTypesQueryHandler(IFieldTypeRegistry registry)
    : IQueryHandler<GetFieldTypesQuery, IReadOnlyList<FieldTypeResponse>>
{
    public Task<Result<IReadOnlyList<FieldTypeResponse>>> HandleAsync(
        GetFieldTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<FieldTypeResponse> items =
        [
            .. registry
                .GetAll()
                .Select(static p => new FieldTypeResponse(
                    p.Key,
                    p.SystemGuid,
                    p.Name,
                    p.Description,
                    p.EditorKind,
                    [
                        .. p.Qualifiers.Select(static q => new FieldQualifierDefinitionResponse(
                            q.Key,
                            q.Name,
                            q.Description,
                            q.DefaultValue
                        )),
                    ]
                )),
        ];

        return Task.FromResult(Result.Success(items));
    }
}
