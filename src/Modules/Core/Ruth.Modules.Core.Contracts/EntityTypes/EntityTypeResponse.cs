namespace Ruth.Modules.Core.Contracts.EntityTypes;

/// <summary>Represents a compile-time discovered entity type registered in the Core entity registry.</summary>
public sealed record EntityTypeResponse(
    Guid Id,
    Guid SystemGuid,
    string Key,
    string Name,
    string FriendlyName,
    string ModuleName,
    bool IsEntity,
    bool IsSecured,
    bool SupportsAttributes
);
