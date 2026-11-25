using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Duplicates;

/// <summary>Query to resolve a potentially retired PersonId or external record GUID to its canonical surviving PersonId.</summary>
public sealed record ResolveCanonicalPersonIdQuery(Guid PersonOrExternalId)
    : IQuery<CanonicalPersonResolutionResponse>;
