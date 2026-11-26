using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Media;

/// <summary>Command to synchronize folders and media elements from an external media provider.</summary>
public sealed record SyncMediaAccountCommand(Guid MediaAccountId) : ICommand<MediaAccountResponse>;
