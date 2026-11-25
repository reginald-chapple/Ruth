using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Duplicates;

/// <summary>Query to resolve a potentially retired PersonId or legacy PersonAlias GUID to its canonical surviving PersonId.</summary>
public sealed record ResolveCanonicalPersonIdQuery(Guid PersonOrAliasId)
    : IQuery<CanonicalPersonResolutionResponse>;
