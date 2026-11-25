using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Preferences;

/// <summary>Lists stored preferences for an owner, optionally filtered by key prefix or entity scope.</summary>
public sealed record GetPersonPreferencesQuery(
    Guid OwnerKey,
    string? KeyPrefix = null,
    Guid? EntityTypeId = null,
    Guid? EntityId = null
) : IQuery<IReadOnlyList<PersonPreferenceResponse>>;
