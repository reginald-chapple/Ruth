namespace Ruth.Modules.Core.Contracts.Preferences;

/// <summary>User/person key-value preference entry optionally scoped to an entity.</summary>
public sealed record PersonPreferenceResponse(
    Guid Id,
    Guid OwnerKey,
    string Key,
    Guid? EntityTypeId,
    Guid? EntityId,
    string Value,
    bool IsEnduring,
    DateTimeOffset LastAccessedOnUtc
);
