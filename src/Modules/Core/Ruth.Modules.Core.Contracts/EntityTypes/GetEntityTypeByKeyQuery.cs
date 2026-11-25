using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.EntityTypes;

/// <summary>Looks up a single registered entity type by its canonical key.</summary>
public sealed record GetEntityTypeByKeyQuery(string Key) : IQuery<EntityTypeResponse>;
