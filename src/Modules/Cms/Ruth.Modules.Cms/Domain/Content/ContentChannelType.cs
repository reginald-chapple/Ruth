using Ruth.Modules.Cms.Contracts.Content;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Content;

/// <summary>Schema template defining scheduling and moderation behavior for CMS content channels.</summary>
public sealed class ContentChannelType
    : AggregateRoot<ContentChannelTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private ContentChannelType() { }

    private ContentChannelType(ContentChannelTypeId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public ContentChannelDateType DateRangeType { get; private set; }
    public bool DisablePriority { get; private set; }
    public bool DisableStatus { get; private set; }
    public bool IncludeTime { get; private set; } = true;
    public bool IsSystem { get; private set; }

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

    public static Result<ContentChannelType> Create(
        string name,
        string key,
        ContentChannelDateType dateRangeType = ContentChannelDateType.DateRange,
        bool disablePriority = false,
        bool disableStatus = false,
        bool includeTime = true,
        bool isSystem = false
    )
    {
        var normalizedKey = CmsTextNormalizer.NormalizeKey(key);
        return string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(normalizedKey)
            ? Result.Failure<ContentChannelType>(
                CmsErrors.Validation(
                    "ContentChannelType.Invalid",
                    "Content channel type name and key are required."
                )
            )
            : Result.Success(
                new ContentChannelType(ContentChannelTypeId.Create())
                {
                    Name = name.Trim(),
                    Key = normalizedKey,
                    DateRangeType = dateRangeType,
                    DisablePriority = disablePriority,
                    DisableStatus = disableStatus,
                    IncludeTime = includeTime,
                    IsSystem = isSystem,
                }
            );
    }
}
