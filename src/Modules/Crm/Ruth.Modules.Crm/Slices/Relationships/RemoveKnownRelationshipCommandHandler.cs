using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Relationships;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.Modules.Crm.Infrastructure.Graph;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Relationships;

internal sealed class RemoveKnownRelationshipCommandHandler(
    CrmDbContext dbContext,
    ICrmGraphService graphService
) : ICommandHandler<RemoveKnownRelationshipCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        RemoveKnownRelationshipCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = KnownRelationshipId.From(command.RelationshipId);
        var forward = await dbContext.KnownRelationships.FirstOrDefaultAsync(
            r => r.Id == id,
            cancellationToken
        );
        if (forward is null)
        {
            return CrmErrors.RelationshipNotFound;
        }

        KnownRelationship? inverse = null;
        if (forward.InverseRelationshipId.HasValue)
        {
            inverse = await dbContext.KnownRelationships.FirstOrDefaultAsync(
                r => r.Id == forward.InverseRelationshipId.Value,
                cancellationToken
            );
        }

        _ = dbContext.KnownRelationships.Remove(forward);
        if (inverse is not null)
        {
            _ = dbContext.KnownRelationships.Remove(inverse);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await graphService.RemoveKnownRelationshipEdgeAsync(
            forward.OwnerPersonId.Value,
            forward.RelatedPersonId.Value,
            forward.RelationshipType.ToString(),
            cancellationToken
        );
        if (inverse is not null)
        {
            await graphService.RemoveKnownRelationshipEdgeAsync(
                inverse.OwnerPersonId.Value,
                inverse.RelatedPersonId.Value,
                inverse.RelationshipType.ToString(),
                cancellationToken
            );
        }

        return Result.Success(value: true);
    }
}
