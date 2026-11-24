namespace Ruth.SharedKernel.Events;

/// <summary>Base record for domain events carrying a UUIDv7 identifier and UTC timestamp.</summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>Initializes a domain event with the current UTC timestamp.</summary>
    protected DomainEvent()
        : this(DateTimeOffset.UtcNow) { }

    /// <summary>Initializes a domain event with an explicit UTC timestamp (e.g., from <see cref="Time.IClock" />).</summary>
    /// <param name="occurredOnUtc">The UTC instant when the event occurred.</param>
    protected DomainEvent(DateTimeOffset occurredOnUtc)
    {
        EventId = Guid.CreateVersion7(occurredOnUtc);
        OccurredOnUtc = occurredOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public Guid EventId { get; init; }

    /// <inheritdoc />
    public DateTimeOffset OccurredOnUtc { get; init; }
}
