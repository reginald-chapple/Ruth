namespace Ruth.Modules.Crm.Contracts.Signals;

/// <summary>Represents an active or expired signal attached to a person.</summary>
public sealed record PersonSignalResponse(
    Guid Id,
    Guid PersonId,
    Guid SignalTypeId,
    string SignalTypeName,
    string SignalColor,
    string SignalIcon,
    int PriorityOrder,
    Guid OwnerPersonId,
    string? Note,
    DateTimeOffset? ExpirationDateUtc,
    bool IsActive,
    DateTimeOffset CreatedOnUtc
);
