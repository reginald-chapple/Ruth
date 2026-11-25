using Ruth.Modules.Crm.Domain.People;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Crm.Domain.Badges;

/// <summary>Configured badge slot rendered on the Person 360 profile badge bar.</summary>
public sealed class BadgeDefinition
    : AggregateRoot<BadgeDefinitionId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for BadgeDefinition.</summary>
    public const string EntityTypeKeyName = "Ruth.Crm.BadgeDefinition";

    private Dictionary<string, string?> _settings = new(StringComparer.OrdinalIgnoreCase);

    private BadgeDefinition() { }

    private BadgeDefinition(
        BadgeDefinitionId id,
        string name,
        string? description,
        string badgeComponentKey,
        string entityTypeKey,
        bool isActive,
        int order,
        IReadOnlyDictionary<string, string?>? settings
    )
        : base(id)
    {
        Name = name;
        Description = description;
        BadgeComponentKey = badgeComponentKey;
        TargetEntityTypeKey = entityTypeKey;
        IsActive = isActive;
        Order = Math.Max(0, order);
        if (settings is not null)
        {
            _settings = new Dictionary<string, string?>(settings, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>Gets the badge display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional badge description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the DI-registered <see cref="Contracts.Badges.IPersonBadgeEvaluator" /> key.</summary>
    public string BadgeComponentKey { get; private set; } = string.Empty;

    /// <summary>Gets the target entity type key (defaults to Person).</summary>
    public string TargetEntityTypeKey { get; private set; } = Person.EntityTypeKeyName;

    /// <summary>Gets whether this badge definition is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets the display order.</summary>
    public int Order { get; private set; }

    /// <summary>Gets optional JSONB configuration settings for the badge evaluator.</summary>
    public IReadOnlyDictionary<string, string?> Settings
    {
        get => _settings;
        private set =>
            _settings = value is null
                ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string?>(value, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Creates a validated badge definition.</summary>
    public static Result<BadgeDefinition> Create(
        string name,
        string badgeComponentKey,
        string? description = null,
        bool isActive = true,
        int order = 0,
        IReadOnlyDictionary<string, string?>? settings = null,
        BadgeDefinitionId? explicitId = null
    )
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(badgeComponentKey))
        {
            return CrmErrors.BadgeNameRequired;
        }

        var desc = CrmTextPolicy.ValidateOptionalText(
            description,
            CrmTextPolicy.DescriptionMaxLength
        );
        return desc.IsFailure
            ? Result.Failure<BadgeDefinition>(desc.Errors)
            : Result.Success(
                new BadgeDefinition(
                    explicitId ?? BadgeDefinitionId.Create(),
                    name.Trim(),
                    desc.Value,
                    badgeComponentKey.Trim().ToLowerInvariant(),
                    Person.EntityTypeKeyName,
                    isActive,
                    order,
                    settings
                )
            );
    }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }
}
