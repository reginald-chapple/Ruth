namespace Ruth.SharedKernel.Events;

/// <summary>Represents a cross-module contract event published across bounded contexts.</summary>
public interface IIntegrationEvent
{
    /// <summary>Gets the unique UUIDv7 identifier for this integration event.</summary>
    Guid EventId { get; }

    /// <summary>Gets the UTC timestamp when this integration event was produced.</summary>
    DateTimeOffset OccurredOnUtc { get; }
}
