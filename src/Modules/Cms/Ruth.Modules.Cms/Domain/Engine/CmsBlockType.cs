using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>Catalog entry describing an available IRuthBlock component type.</summary>
public sealed class CmsBlockType
    : AggregateRoot<CmsBlockTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private Dictionary<string, string?> _defaultSettings = new(StringComparer.OrdinalIgnoreCase);

    private CmsBlockType() { }

    private CmsBlockType(CmsBlockTypeId id)
        : base(id) { }

    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = "General";
    public string? Description { get; private set; }
    public bool RequiresInteractivity { get; private set; }
    public bool IsSystem { get; private set; }

    public IReadOnlyDictionary<string, string?> DefaultSettings
    {
        get => _defaultSettings;
        private set =>
            _defaultSettings = new Dictionary<string, string?>(
                value,
                StringComparer.OrdinalIgnoreCase
            );
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

    public static Result<CmsBlockType> Create(
        string key,
        string name,
        string category = "General",
        string? description = null,
        bool requiresInteractivity = false,
        IReadOnlyDictionary<string, string?>? defaultSettings = null,
        bool isSystem = false
    )
    {
        var normalizedKey = CmsTextNormalizer.NormalizeKey(key);
        if (string.IsNullOrWhiteSpace(normalizedKey) || string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<CmsBlockType>(
                CmsErrors.Validation("BlockType.Invalid", "BlockType key and name are required.")
            );
        }

        var settings = defaultSettings is null
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(defaultSettings, StringComparer.OrdinalIgnoreCase);

        return Result.Success(
            new CmsBlockType(CmsBlockTypeId.Create())
            {
                Key = normalizedKey,
                Name = name.Trim(),
                Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                RequiresInteractivity = requiresInteractivity,
                IsSystem = isSystem,
                _defaultSettings = settings,
            }
        );
    }
}
