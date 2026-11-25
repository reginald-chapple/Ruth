namespace Ruth.SharedKernel.Metadata;

/// <summary>Compile-time descriptor for a bounded-context entity registered into Core's entity type registry.</summary>
public sealed record EntityTypeRegistration(
    Guid SystemGuid,
    string Key,
    string Name,
    string FriendlyName,
    string ModuleName,
    bool IsEntity,
    bool IsSecured,
    bool SupportsAttributes
);
