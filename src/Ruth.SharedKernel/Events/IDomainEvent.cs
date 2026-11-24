namespace Ruth.SharedKernel.Events;

/// <summary>Represents a fact that occurred within a bounded context's domain model.</summary>
public interface IDomainEvent
{
    /// <summary>Gets the unique UUIDv7 identifier for this domain event instance.</summary>
    Guid EventId { get; }

    /// <summary>Gets the UTC timestamp when this domain event occurred.</summary>
    DateTimeOffset OccurredOnUtc { get; }
}
