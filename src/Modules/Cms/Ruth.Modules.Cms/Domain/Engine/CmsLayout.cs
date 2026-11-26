using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>Site layout defining the structural template and named content zones.</summary>
public sealed class CmsLayout : AggregateRoot<CmsLayoutId>, IAuditableEntity, IHasConcurrencyToken
{
    private static readonly string[] _defaultZones =
    [
        "Header",
        "Hero",
        "Main",
        "Sidebar",
        "Footer",
    ];

    private List<string> _zones = [.. _defaultZones];

    private CmsLayout() { }

    private CmsLayout(CmsLayoutId id)
        : base(id) { }

    public CmsSiteId SiteId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string FileName { get; private set; } = "FullWidth";
    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<string> Zones
    {
        get => _zones;
        private set => _zones = [.. value];
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

    public static Result<CmsLayout> Create(
        CmsSiteId siteId,
        string name,
        string slug,
        string fileName = "FullWidth",
        string? description = null,
        IReadOnlyList<string>? zones = null
    )
    {
        if (siteId.IsEmpty || string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<CmsLayout>(
                CmsErrors.Validation("Layout.Invalid", "SiteId and layout name are required.")
            );
        }

        var normalizedSlug = CmsTextNormalizer.NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return Result.Failure<CmsLayout>(
                CmsErrors.Validation("Layout.SlugInvalid", "Layout slug is invalid.")
            );
        }

        List<string> normalizedZones = zones is { Count: > 0 }
            ?
            [
                .. zones
                    .Where(static z => !string.IsNullOrWhiteSpace(z))
                    .Select(static z => z.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase),
            ]
            : [.. _defaultZones];

        if (normalizedZones.Count == 0)
        {
            normalizedZones = [.. _defaultZones];
        }

        return Result.Success(
            new CmsLayout(CmsLayoutId.Create())
            {
                SiteId = siteId,
                Name = name.Trim(),
                Slug = normalizedSlug,
                FileName = string.IsNullOrWhiteSpace(fileName) ? "FullWidth" : fileName.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                _zones = normalizedZones,
                IsActive = true,
            }
        );
    }

    public bool HasZone(string zoneName)
    {
        return _zones.Exists(z => string.Equals(z, zoneName, StringComparison.OrdinalIgnoreCase));
    }
}
