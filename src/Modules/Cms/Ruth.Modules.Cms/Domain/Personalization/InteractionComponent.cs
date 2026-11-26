using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Personalization;

/// <summary>Tracked entity/surface (page, campaign, mobile screen, adaptation) inside an <see cref="InteractionChannel" />.</summary>
public sealed class InteractionComponent
    : AggregateRoot<InteractionComponentId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private Dictionary<string, string?> _componentData = new(StringComparer.OrdinalIgnoreCase);

    private InteractionComponent() { }

    private InteractionComponent(InteractionComponentId id)
        : base(id) { }

    public InteractionChannelId InteractionChannelId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ComponentKey { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }

    public IReadOnlyDictionary<string, string?> ComponentData
    {
        get => _componentData;
        private set =>
            _componentData = new Dictionary<string, string?>(
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

    public static Result<InteractionComponent> Create(
        InteractionChannelId channelId,
        string componentKey,
        string name,
        Guid? entityId = null
    )
    {
        var normalizedKey = CmsTextNormalizer.NormalizeKey(componentKey);
        return
            channelId.IsEmpty
            || string.IsNullOrWhiteSpace(normalizedKey)
            || string.IsNullOrWhiteSpace(name)
            ? Result.Failure<InteractionComponent>(
                CmsErrors.Validation(
                    "InteractionComponent.Invalid",
                    "InteractionChannelId, ComponentKey, and Name are required."
                )
            )
            : Result.Success(
                new InteractionComponent(InteractionComponentId.Create())
                {
                    InteractionChannelId = channelId,
                    ComponentKey = normalizedKey,
                    Name = name.Trim(),
                    EntityId = entityId,
                }
            );
    }
}
