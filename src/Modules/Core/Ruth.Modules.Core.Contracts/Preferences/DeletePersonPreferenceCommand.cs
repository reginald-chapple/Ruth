using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Preferences;

/// <summary>Deletes a specific user/person preference by owner and key.</summary>
public sealed record DeletePersonPreferenceCommand(Guid OwnerKey, string Key) : ICommand<bool>;
