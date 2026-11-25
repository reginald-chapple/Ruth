using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Crm.Infrastructure.Graph;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Relationships;

internal sealed class GetPersonRelationshipGraphQueryHandler(ICrmGraphService graphService)
    : IQueryHandler<GetPersonRelationshipGraphQuery, PersonRelationshipGraphResponse>
{
    public async Task<Result<PersonRelationshipGraphResponse>> HandleAsync(
        GetPersonRelationshipGraphQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var graph = await graphService.TraverseAsync(
            query.PersonId,
            query.MaxHops,
            cancellationToken
        );
        return Result.Success(graph);
    }
}
