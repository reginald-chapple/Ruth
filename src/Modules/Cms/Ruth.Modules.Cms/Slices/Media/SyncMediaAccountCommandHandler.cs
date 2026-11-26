using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Media;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Media;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Media;

internal sealed class SyncMediaAccountCommandHandler(
    CmsDbContext db,
    IEnumerable<IMediaAccountSyncProvider> providers,
    IClock clock
) : ICommandHandler<SyncMediaAccountCommand, MediaAccountResponse>
{
    public async Task<Result<MediaAccountResponse>> HandleAsync(
        SyncMediaAccountCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var accountId = MediaAccountId.From(command.MediaAccountId);
        var account = await db.MediaAccounts.FirstOrDefaultAsync(
            a => a.Id == accountId,
            cancellationToken
        );
        if (account is null)
        {
            return Result.Failure<MediaAccountResponse>(
                CmsErrors.NotFound("MediaAccount", command.MediaAccountId.ToString("D"))
            );
        }

        var provider = providers.FirstOrDefault(p =>
            string.Equals(p.ProviderKey, account.ProviderKey, StringComparison.OrdinalIgnoreCase)
        );
        if (provider is null)
        {
            return Result.Failure<MediaAccountResponse>(
                CmsErrors.NotFound("MediaSyncProvider", account.ProviderKey)
            );
        }

        var syncedFolders = await provider.FetchFoldersAsync(
            account.AccountIdentifier,
            account.Settings,
            cancellationToken
        );
        var folderIds = await UpsertFoldersAsync(accountId, syncedFolders, cancellationToken);

        account.MarkSynced(clock.UtcNow);
        _ = await db.SaveChangesAsync(cancellationToken);

        var totalElements = await db
            .MediaElements.AsNoTracking()
            .CountAsync(e => folderIds.Contains(e.MediaFolderId), cancellationToken);

        return Result.Success(
            new MediaAccountResponse(
                account.Id.Value,
                account.Name,
                account.Slug,
                account.ProviderKey,
                account.AccountIdentifier,
                account.IsActive,
                account.LastSyncedUtc,
                folderIds.Count,
                totalElements
            )
        );
    }

    private async Task<List<MediaFolderId>> UpsertFoldersAsync(
        MediaAccountId accountId,
        IReadOnlyList<SyncedMediaFolderSpec> syncedFolders,
        CancellationToken ct
    )
    {
        var existingFolders = await db
            .MediaFolders.Where(f => f.MediaAccountId == accountId)
            .ToDictionaryAsync(
                static f => f.ExternalFolderKey,
                StringComparer.OrdinalIgnoreCase,
                ct
            );

        foreach (var folderSpec in syncedFolders)
        {
            if (!existingFolders.TryGetValue(folderSpec.ExternalFolderKey, out var folder))
            {
                var createFolderRes = MediaFolder.Create(
                    accountId,
                    folderSpec.Name,
                    folderSpec.ExternalFolderKey,
                    folderSpec.Description
                );
                if (createFolderRes.IsFailure)
                {
                    continue;
                }

                folder = createFolderRes.Value;
                _ = db.MediaFolders.Add(folder);
                existingFolders[folder.ExternalFolderKey] = folder;
            }
            else
            {
                folder.UpdateFromSync(folderSpec.Name, folderSpec.Description);
            }

            await UpsertElementsAsync(folder.Id, folderSpec.Elements, ct);
        }

        return [.. existingFolders.Values.Select(static f => f.Id)];
    }

    private async Task UpsertElementsAsync(
        MediaFolderId folderId,
        IReadOnlyList<SyncedMediaElementSpec> specs,
        CancellationToken ct
    )
    {
        var existing = await db
            .MediaElements.Where(e => e.MediaFolderId == folderId)
            .ToDictionaryAsync(
                static e => e.ExternalElementKey,
                StringComparer.OrdinalIgnoreCase,
                ct
            );

        foreach (var spec in specs)
        {
            if (existing.TryGetValue(spec.ExternalElementKey, out var current))
            {
                current.UpdateFromSync(spec);
            }
            else
            {
                var created = MediaElement.Create(folderId, spec);
                if (created.IsSuccess)
                {
                    _ = db.MediaElements.Add(created.Value);
                    existing[created.Value.ExternalElementKey] = created.Value;
                }
            }
        }
    }
}
