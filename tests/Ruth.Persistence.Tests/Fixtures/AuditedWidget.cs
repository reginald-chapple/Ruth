using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Persistence.Tests.Fixtures;

public sealed class AuditedWidget
    : AggregateRoot<TestWidgetId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        IHasIntegrationEvents
{
    private readonly List<IIntegrationEvent> _integrationEvents = [];

    private AuditedWidget() { }

    public AuditedWidget(TestWidgetId id, string name, DateTimeOffset effectiveAt)
        : base(id)
    {
        Name = name;
        EffectiveAt = effectiveAt;
    }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset EffectiveAt { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public Guid ConcurrencyStamp { get; private set; }

    public IReadOnlyCollection<IIntegrationEvent> IntegrationEvents =>
        _integrationEvents.AsReadOnly();

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

    public void ClearIntegrationEvents()
    {
        _integrationEvents.Clear();
    }

    public void Rename(string newName, bool emitIntegrationEvent = false)
    {
        Name = newName;
        RaiseDomainEvent(new TestWidgetDomainEvent(Id, newName));

        if (emitIntegrationEvent)
        {
            _integrationEvents.Add(new TestWidgetIntegrationEvent(Id.Value, $"Renamed:{newName}"));
        }
    }

    public void RenameWithDualEvent(string newName)
    {
        Name = newName;
        RaiseDomainEvent(new TestWidgetDualEvent(Id, newName));
    }

    public void AddIntegrationEvent(IIntegrationEvent integrationEvent)
    {
        _integrationEvents.Add(integrationEvent);
    }
}
