namespace Ruth.Modules.Crm.Contracts.Signals;

/// <summary>Represents a configured safety/VIP signal type with priority ordering.</summary>
public sealed record SignalTypeResponse(
    Guid Id,
    string Name,
    string? Description,
    string SignalColor,
    string SignalIcon,
    int Order
);
