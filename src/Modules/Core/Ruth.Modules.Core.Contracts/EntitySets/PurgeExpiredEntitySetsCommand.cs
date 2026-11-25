using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.EntitySets;

/// <summary>Purges expired ephemeral entity sets and returns the count removed.</summary>
public sealed record PurgeExpiredEntitySetsCommand : ICommand<int>;
