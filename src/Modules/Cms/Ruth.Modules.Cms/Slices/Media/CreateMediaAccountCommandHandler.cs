using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Media;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Media;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Media;

internal sealed class CreateMediaAccountCommandHandler(CmsDbContext db)
    : ICommandHandler<CreateMediaAccountCommand, MediaAccountResponse>
{
    public async Task<Result<MediaAccountResponse>> HandleAsync(
        CreateMediaAccountCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var res = MediaAccount.Create(
            command.Name,
            command.Slug,
            command.ProviderKey,
            command.AccountIdentifier,
            command.Settings,
            command.IsActive
        );
        if (res.IsFailure)
        {
            return Result.Failure<MediaAccountResponse>(res.PrimaryError);
        }

        var exists = await db
            .MediaAccounts.AsNoTracking()
            .AnyAsync(a => a.Slug == res.Value.Slug, cancellationToken);
        if (exists)
        {
            return Result.Failure<MediaAccountResponse>(
                CmsErrors.Conflict(
                    "MediaAccount.DuplicateSlug",
                    $"MediaAccount slug '{res.Value.Slug}' already exists."
                )
            );
        }

        _ = db.MediaAccounts.Add(res.Value);
        _ = await db.SaveChangesAsync(cancellationToken);

        return Result.Success(
            new MediaAccountResponse(
                res.Value.Id.Value,
                res.Value.Name,
                res.Value.Slug,
                res.Value.ProviderKey,
                res.Value.AccountIdentifier,
                res.Value.IsActive,
                res.Value.LastSyncedUtc,
                0,
                0
            )
        );
    }
}
