using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Media;

/// <summary>Configured external media provider account (Vimeo, YouTube, HLS, or in-memory).</summary>
public sealed class MediaAccount
    : AggregateRoot<MediaAccountId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private Dictionary<string, string?> _settings = new(StringComparer.OrdinalIgnoreCase);

    private MediaAccount() { }

    private MediaAccount(MediaAccountId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string ProviderKey { get; private set; } = string.Empty;
    public string AccountIdentifier { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? LastSyncedUtc { get; private set; }

    public IReadOnlyDictionary<string, string?> Settings
    {
        get => _settings;
        private set =>
            _settings = new Dictionary<string, string?>(value, StringComparer.OrdinalIgnoreCase);
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

    public static Result<MediaAccount> Create(
        string name,
        string slug,
        string providerKey,
        string accountIdentifier,
        IReadOnlyDictionary<string, string?>? settings = null,
        bool isActive = true
    )
    {
        var normalizedSlug = CmsTextNormalizer.NormalizeSlug(slug);
        var normalizedProvider = CmsTextNormalizer.NormalizeKey(providerKey);
        return
            string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(normalizedSlug)
            || string.IsNullOrWhiteSpace(normalizedProvider)
            || string.IsNullOrWhiteSpace(accountIdentifier)
            ? Result.Failure<MediaAccount>(
                CmsErrors.Validation(
                    "MediaAccount.Invalid",
                    "Name, Slug, ProviderKey, and AccountIdentifier are required."
                )
            )
            : Result.Success(
                new MediaAccount(MediaAccountId.Create())
                {
                    Name = name.Trim(),
                    Slug = normalizedSlug,
                    ProviderKey = normalizedProvider,
                    AccountIdentifier = accountIdentifier.Trim(),
                    IsActive = isActive,
                    _settings = settings is null
                        ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                        : new Dictionary<string, string?>(
                            settings,
                            StringComparer.OrdinalIgnoreCase
                        ),
                }
            );
    }

    public void MarkSynced(DateTimeOffset syncedAtUtc)
    {
        LastSyncedUtc = syncedAtUtc.ToUniversalTime();
    }
}
