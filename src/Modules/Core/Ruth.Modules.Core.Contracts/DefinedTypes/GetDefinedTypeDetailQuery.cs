using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.DefinedTypes;

/// <summary>Retrieves a DefinedType and its ordered values by identifier.</summary>
public sealed record GetDefinedTypeDetailQuery(Guid DefinedTypeId)
    : IQuery<DefinedTypeDetailResponse>;
