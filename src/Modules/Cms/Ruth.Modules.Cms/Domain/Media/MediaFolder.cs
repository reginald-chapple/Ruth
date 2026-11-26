using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Media;

/// <summary>Synchronized folder/collection belonging to a <see cref="MediaAccount" />.</summary>
public sealed class MediaFolder
    : AggregateRoot<MediaFolderId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private MediaFolder() { }

    private MediaFolder(MediaFolderId id)
        : base(id) { }

    public MediaAccountId MediaAccountId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ExternalFolderKey { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ContentChannelId? ContentChannelId { get; private set; }
    public bool IsPublic { get; private set; } = true;
    public bool IsContentChannelSyncEnabled { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public static Result<MediaFolder> Create(
        MediaAccountId mediaAccountId,
        string name,
        string externalFolderKey,
        string? description = null,
        ContentChannelId? contentChannelId = null,
        bool isPublic = true,
        bool isContentChannelSyncEnabled = false
    )
    {
        return
            mediaAccountId.IsEmpty
            || string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(externalFolderKey)
            ? Result.Failure<MediaFolder>(
                CmsErrors.Validation(
                    "MediaFolder.Invalid",
                    "MediaAccountId, Name, and ExternalFolderKey are required."
                )
            )
            : Result.Success(
                new MediaFolder(MediaFolderId.Create())
                {
                    MediaAccountId = mediaAccountId,
                    Name = name.Trim(),
                    ExternalFolderKey = externalFolderKey.Trim(),
                    Description = string.IsNullOrWhiteSpace(description)
                        ? null
                        : description.Trim(),
                    ContentChannelId = contentChannelId,
                    IsPublic = isPublic,
                    IsContentChannelSyncEnabled = isContentChannelSyncEnabled,
                }
            );
    }

    public void UpdateFromSync(string name, string? description)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name.Trim();
        }

        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
