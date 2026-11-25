using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Domain.Relationships;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.Modules.Crm.Infrastructure.Graph;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Relationships;

internal sealed class AddKnownRelationshipCommandHandler(
    CrmDbContext dbContext,
    ICrmGraphService graphService
) : ICommandHandler<AddKnownRelationshipCommand, KnownRelationshipResponse>
{
    public async Task<Result<KnownRelationshipResponse>> HandleAsync(
        AddKnownRelationshipCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var ownerId = PersonId.From(command.OwnerPersonId);
        var relatedId = PersonId.From(command.RelatedPersonId);

        var owner = await dbContext
            .People.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == ownerId, cancellationToken);
        var related = await dbContext
            .People.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == relatedId, cancellationToken);
        if (owner is null || related is null)
        {
            return CrmErrors.PersonNotFound;
        }

        var exists = await dbContext.KnownRelationships.AnyAsync(
            r =>
                r.OwnerPersonId == ownerId
                && r.RelatedPersonId == relatedId
                && r.RelationshipType == command.RelationshipType,
            cancellationToken
        );
        if (exists)
        {
            return CrmErrors.RelationshipAlreadyExists;
        }

        var pairResult = KnownRelationship.CreatePair(
            ownerId,
            relatedId,
            command.RelationshipType,
            command.Notes
        );
        if (pairResult.IsFailure)
        {
            return Result.Failure<KnownRelationshipResponse>(pairResult.Errors);
        }

        var (forward, inverse) = pairResult.Value;
        await PersistAndSyncGraphAsync(owner, related, forward, inverse, cancellationToken);

        return Result.Success(
            new KnownRelationshipResponse(
                forward.Id.Value,
                forward.OwnerPersonId.Value,
                forward.RelatedPersonId.Value,
                related.FullName,
                forward.RelationshipType,
                forward.InverseRelationshipId?.Value,
                forward.Notes,
                forward.CreatedOnUtc
            )
        );
    }

    private async Task PersistAndSyncGraphAsync(
        Person owner,
        Person related,
        KnownRelationship forward,
        KnownRelationship inverse,
        CancellationToken cancellationToken
    )
    {
        _ = dbContext.KnownRelationships.Add(forward);
        _ = dbContext.KnownRelationships.Add(inverse);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await graphService.UpsertKnownRelationshipEdgeAsync(
            owner.Id.Value,
            owner.FullName,
            related.Id.Value,
            related.FullName,
            forward.RelationshipType.ToString(),
            cancellationToken
        );
        await graphService.UpsertKnownRelationshipEdgeAsync(
            related.Id.Value,
            related.FullName,
            owner.Id.Value,
            owner.FullName,
            inverse.RelationshipType.ToString(),
            cancellationToken
        );
    }
}
