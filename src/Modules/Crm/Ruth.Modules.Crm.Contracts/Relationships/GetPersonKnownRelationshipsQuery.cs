using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Query to list all known relationships for a person.</summary>
public sealed record GetPersonKnownRelationshipsQuery(Guid PersonId)
    : IQuery<IReadOnlyList<KnownRelationshipResponse>>;
