using Ruth.Modules.Core.Contracts.EntitySets;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.EntitySets;

internal sealed class CreateEntitySetCommandHandler(CoreDbContext dbContext, IClock clock)
    : ICommandHandler<CreateEntitySetCommand, EntitySetResponse>
{
    public async Task<Result<EntitySetResponse>> HandleAsync(
        CreateEntitySetCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var nowUtc = clock.UtcNow;
        var created = EntitySet.Create(
            command.Name,
            command.EntityTypeId.HasValue
                ? EntityTypeDescriptorId.From(command.EntityTypeId.Value)
                : null,
            command.EntityIds,
            command.ExpireInMinutes,
            nowUtc,
            command.ParentEntitySetId.HasValue
                ? EntitySetId.From(command.ParentEntitySetId.Value)
                : null,
            command.PurposeValueId,
            command.Note,
            command.Order
        );
        if (created.IsFailure)
        {
            return Result.Failure<EntitySetResponse>(created.Errors);
        }

        _ = dbContext.EntitySets.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(EntitySetMapper.ToResponse(created.Value, nowUtc));
    }
}
