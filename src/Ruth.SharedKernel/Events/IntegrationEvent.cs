namespace Ruth.SharedKernel.Events;

/// <summary>Base record for cross-module integration events carrying a UUIDv7 identifier and UTC timestamp.</summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    /// <summary>Initializes an integration event with the current UTC timestamp.</summary>
    protected IntegrationEvent()
        : this(DateTimeOffset.UtcNow) { }

    /// <summary>Initializes an integration event with an explicit UTC timestamp.</summary>
    /// <param name="occurredOnUtc">The UTC instant when the event was created.</param>
    protected IntegrationEvent(DateTimeOffset occurredOnUtc)
    {
        EventId = Guid.CreateVersion7(occurredOnUtc);
        OccurredOnUtc = occurredOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public Guid EventId { get; init; }

    /// <inheritdoc />
    public DateTimeOffset OccurredOnUtc { get; init; }
}
