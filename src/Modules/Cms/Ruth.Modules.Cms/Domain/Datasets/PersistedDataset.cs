using Ruth.Modules.Cms.Contracts.Datasets;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Datasets;

/// <summary>Cached JSON dataset materialized on schedule or demand for Razor CMS blocks and Minimal APIs.</summary>
public sealed class PersistedDataset
    : AggregateRoot<PersistedDatasetId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private Dictionary<string, string?> _sourceConfiguration = new(
        StringComparer.OrdinalIgnoreCase
    );

    private PersistedDataset() { }

    private PersistedDataset(PersistedDatasetId id)
        : base(id) { }

    public string AccessKey { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PersistedDatasetSourceKind DataSourceKind { get; private set; }
    public int RefreshIntervalMinutes { get; private set; } = 60;
    public DateTimeOffset? ExpireDateTimeUtc { get; private set; }
    public int MemoryCacheDurationMinutes { get; private set; } = 15;
    public string? ResultDataJson { get; private set; }
    public DateTimeOffset? LastRefreshDateTimeUtc { get; private set; }
    public long TimeToBuildMs { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool AllowManualRefresh { get; private set; } = true;

    public IReadOnlyDictionary<string, string?> SourceConfiguration
    {
        get => _sourceConfiguration;
        private set =>
            _sourceConfiguration = new Dictionary<string, string?>(
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

    public static Result<PersistedDataset> Create(
        string accessKey,
        string name,
        PersistedDatasetSourceKind dataSourceKind,
        string? description = null,
        IReadOnlyDictionary<string, string?>? sourceConfiguration = null,
        int refreshIntervalMinutes = 60,
        DateTimeOffset? expireDateTimeUtc = null,
        int memoryCacheDurationMinutes = 15,
        bool allowManualRefresh = true
    )
    {
        var normalizedKey = CmsTextNormalizer.NormalizeKey(accessKey);
        return string.IsNullOrWhiteSpace(normalizedKey) || string.IsNullOrWhiteSpace(name)
            ? Result.Failure<PersistedDataset>(
                CmsErrors.Validation("PersistedDataset.Invalid", "AccessKey and Name are required.")
            )
            : Result.Success(
                new PersistedDataset(PersistedDatasetId.Create())
                {
                    AccessKey = normalizedKey,
                    Name = name.Trim(),
                    Description = string.IsNullOrWhiteSpace(description)
                        ? null
                        : description.Trim(),
                    DataSourceKind = dataSourceKind,
                    RefreshIntervalMinutes = Math.Clamp(refreshIntervalMinutes, 1, 10080),
                    ExpireDateTimeUtc = expireDateTimeUtc?.ToUniversalTime(),
                    MemoryCacheDurationMinutes = Math.Clamp(memoryCacheDurationMinutes, 0, 1440),
                    AllowManualRefresh = allowManualRefresh,
                    IsActive = true,
                    _sourceConfiguration = sourceConfiguration is null
                        ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                        : new Dictionary<string, string?>(
                            sourceConfiguration,
                            StringComparer.OrdinalIgnoreCase
                        ),
                }
            );
    }

    public bool IsStaleAt(DateTimeOffset nowUtc)
    {
        return ResultDataJson is null
            || LastRefreshDateTimeUtc is null
            || LastRefreshDateTimeUtc.Value.AddMinutes(RefreshIntervalMinutes) <= nowUtc;
    }

    public void RecordRefresh(string resultJson, long buildMs, DateTimeOffset refreshedAtUtc)
    {
        ResultDataJson = resultJson;
        TimeToBuildMs = Math.Max(0, buildMs);
        LastRefreshDateTimeUtc = refreshedAtUtc.ToUniversalTime();
    }
}
