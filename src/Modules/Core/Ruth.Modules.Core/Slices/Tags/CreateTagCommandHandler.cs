using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Tags;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Tags;

internal sealed class CreateTagCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<CreateTagCommand, TagResponse>
{
    public async Task<Result<TagResponse>> HandleAsync(
        CreateTagCommand command,
        CancellationToken cancellationToken = default
    )
    {
        EntityTypeDescriptorId? etId = command.EntityTypeId.HasValue
            ? EntityTypeDescriptorId.From(command.EntityTypeId.Value)
            : null;
        CategoryId? catId = command.CategoryId.HasValue
            ? CategoryId.From(command.CategoryId.Value)
            : null;

        var created = Tag.Create(
            command.Name,
            etId,
            command.Description,
            catId,
            command.OwnerUserId,
            command.IconName,
            command.BackgroundColor,
            command.Order
        );
        if (created.IsFailure)
        {
            return Result.Failure<TagResponse>(created.Errors);
        }

        var duplicate = await dbContext.Tags.AnyAsync(
            t =>
                t.NormalizedName == created.Value.NormalizedName
                && t.OwnerUserId == created.Value.OwnerUserId
                && t.EntityTypeId == created.Value.EntityTypeId,
            cancellationToken
        );
        if (duplicate)
        {
            return CoreCollaborationErrors.TagDuplicate;
        }

        _ = dbContext.Tags.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(TagMapper.ToResponse(created.Value, 0));
    }
}
