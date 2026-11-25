using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Preferences;

/// <summary>Creates or updates a user/person key-value preference.</summary>
public sealed record SetPersonPreferenceCommand(
    Guid OwnerKey,
    string Key,
    string Value,
    Guid? EntityTypeId = null,
    Guid? EntityId = null,
    bool IsEnduring = false
) : ICommand<PersonPreferenceResponse>;
