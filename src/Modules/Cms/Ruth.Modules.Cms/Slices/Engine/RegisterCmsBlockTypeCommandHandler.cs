using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class RegisterCmsBlockTypeCommandHandler(CmsDbContext db)
    : ICommandHandler<RegisterCmsBlockTypeCommand, CmsBlockTypeResponse>
{
    public async Task<Result<CmsBlockTypeResponse>> HandleAsync(
        RegisterCmsBlockTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var res = CmsBlockType.Create(
            command.Key,
            command.Name,
            command.Category,
            command.Description,
            command.RequiresInteractivity,
            command.DefaultSettings
        );
        if (res.IsFailure)
        {
            return Result.Failure<CmsBlockTypeResponse>(res.PrimaryError);
        }

        var exists = await db
            .BlockTypes.AsNoTracking()
            .AnyAsync(b => b.Key == res.Value.Key, cancellationToken);
        if (exists)
        {
            return Result.Failure<CmsBlockTypeResponse>(
                CmsErrors.Conflict(
                    "BlockType.DuplicateKey",
                    $"BlockType '{res.Value.Key}' already exists."
                )
            );
        }

        _ = db.BlockTypes.Add(res.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsProjectionMapper.ToResponse(res.Value));
    }
}
