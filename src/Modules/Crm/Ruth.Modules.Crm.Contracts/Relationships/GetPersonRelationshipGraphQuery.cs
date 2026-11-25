using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Query to traverse multi-hop family, known-relationship, and peer-network connections via Apache AGE.</summary>
public sealed record GetPersonRelationshipGraphQuery(Guid PersonId, int MaxHops = 2)
    : IQuery<PersonRelationshipGraphResponse>;
