using Ruth.Modules.Cms.Contracts.Media;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Media;

/// <summary>Synchronized video, audio, or HLS media asset inside a <see cref="MediaFolder" />.</summary>
public sealed class MediaElement
    : AggregateRoot<MediaElementId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private Dictionary<string, string?> _metadata = new(StringComparer.OrdinalIgnoreCase);

    private MediaElement() { }

    private MediaElement(MediaElementId id)
        : base(id) { }

    public MediaFolderId MediaFolderId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ExternalElementKey { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int DurationSeconds { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public string? PlaybackUrl { get; private set; }
    public string? HlsManifestUrl { get; private set; }
    public string? CaptionsUrl { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public DateTimeOffset? PublishedDateTimeUtc { get; private set; }

    public IReadOnlyDictionary<string, string?> Metadata
    {
        get => _metadata;
        private set =>
            _metadata = new Dictionary<string, string?>(value, StringComparer.OrdinalIgnoreCase);
    }

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

    public static Result<MediaElement> Create(
        MediaFolderId mediaFolderId,
        SyncedMediaElementSpec spec
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (
            mediaFolderId.IsEmpty
            || string.IsNullOrWhiteSpace(spec.Name)
            || string.IsNullOrWhiteSpace(spec.ExternalElementKey)
        )
        {
            return Result.Failure<MediaElement>(
                CmsErrors.Validation(
                    "MediaElement.Invalid",
                    "MediaFolderId, Name, and ExternalElementKey are required."
                )
            );
        }

        var element = new MediaElement(MediaElementId.Create())
        {
            MediaFolderId = mediaFolderId,
            ExternalElementKey = spec.ExternalElementKey.Trim(),
        };
        element.UpdateFromSync(spec);
        return Result.Success(element);
    }

    public void UpdateFromSync(SyncedMediaElementSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        Name = spec.Name.Trim();
        Description = string.IsNullOrWhiteSpace(spec.Description) ? null : spec.Description.Trim();
        DurationSeconds = Math.Max(0, spec.DurationSeconds);
        ThumbnailUrl = spec.ThumbnailUrl;
        PlaybackUrl = spec.PlaybackUrl;
        HlsManifestUrl = spec.HlsManifestUrl;
        CaptionsUrl = spec.CaptionsUrl;
        Width = spec.Width;
        Height = spec.Height;
        PublishedDateTimeUtc = spec.PublishedDateTimeUtc?.ToUniversalTime();
        _metadata = spec.Metadata is null
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(spec.Metadata, StringComparer.OrdinalIgnoreCase);
    }
}
