using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Domain.Relationships;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.Modules.Crm.Infrastructure.Graph;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Relationships;

internal sealed class UpsertPeerNetworkEdgeCommandHandler(
    CrmDbContext dbContext,
    ICrmGraphService graphService,
    IClock clock
) : ICommandHandler<UpsertPeerNetworkEdgeCommand, PeerNetworkEdgeResponse>
{
    public async Task<Result<PeerNetworkEdgeResponse>> HandleAsync(
        UpsertPeerNetworkEdgeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var sourceId = PersonId.From(command.SourcePersonId);
        var targetId = PersonId.From(command.TargetPersonId);

        var source = await dbContext
            .People.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == sourceId, cancellationToken);
        var target = await dbContext
            .People.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == targetId, cancellationToken);
        if (source is null || target is null)
        {
            return CrmErrors.PersonNotFound;
        }

        var edgeResult = await UpsertEdgeEntityAsync(
            sourceId,
            targetId,
            command,
            cancellationToken
        );
        if (edgeResult.IsFailure)
        {
            return Result.Failure<PeerNetworkEdgeResponse>(edgeResult.Errors);
        }

        var edge = edgeResult.Value;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await SyncEdgeToGraphAsync(source, target, edge, cancellationToken);

        return Result.Success(ToResponse(edge, target.FullName));
    }

    private async Task<Result<PeerNetworkEdge>> UpsertEdgeEntityAsync(
        PersonId sourceId,
        PersonId targetId,
        UpsertPeerNetworkEdgeCommand command,
        CancellationToken cancellationToken
    )
    {
        var nowUtc = clock.UtcNow;
        var existing = await dbContext.PeerNetworkEdges.FirstOrDefaultAsync(
            e =>
                e.SourcePersonId == sourceId
                && e.TargetPersonId == targetId
                && e.RelationshipType == command.RelationshipType
                && e.RelatedEntityId == command.RelatedEntityId,
            cancellationToken
        );

        if (existing is not null)
        {
            existing.Recalculate(
                command.Strength,
                command.RelationshipStartDate,
                command.RelationshipEndDate,
                command.Caption,
                nowUtc,
                command.RolePairMultiplier,
                command.IsGrowthEnabled
            );
            return Result.Success(existing);
        }

        var created = PeerNetworkEdge.Create(
            sourceId,
            targetId,
            command.RelationshipType,
            command.Strength,
            command.Caption,
            command.RelationshipStartDate,
            command.RelationshipEndDate,
            nowUtc,
            command.RelatedEntityId,
            command.ClassificationEntityId,
            command.RolePairMultiplier,
            command.IsGrowthEnabled
        );
        if (created.IsSuccess)
        {
            _ = dbContext.PeerNetworkEdges.Add(created.Value);
        }

        return created;
    }

    private Task SyncEdgeToGraphAsync(
        Person source,
        Person target,
        PeerNetworkEdge edge,
        CancellationToken cancellationToken
    )
    {
        var edgeKey = $"{edge.RelationshipType}:{edge.RelatedEntityId?.ToString("N") ?? "none"}";
        var detail = string.Create(
            CultureInfo.InvariantCulture,
            $"{edge.Caption} (Score {edge.RelationshipScore:0.0})"
        );
        return graphService.UpsertPeerNetworkEdgeAsync(
            source.Id.Value,
            source.FullName,
            target.Id.Value,
            target.FullName,
            edgeKey,
            detail,
            cancellationToken
        );
    }

    private static PeerNetworkEdgeResponse ToResponse(PeerNetworkEdge edge, string targetFullName)
    {
        return new PeerNetworkEdgeResponse(
            edge.Id.Value,
            edge.SourcePersonId.Value,
            edge.TargetPersonId.Value,
            targetFullName,
            edge.RelationshipType,
            edge.RelatedEntityId,
            edge.Strength,
            edge.RelationshipScore,
            edge.Trend,
            edge.Caption,
            edge.RelationshipStartDate,
            edge.RelationshipEndDate,
            edge.LastUpdatedOnUtc
        );
    }
}
