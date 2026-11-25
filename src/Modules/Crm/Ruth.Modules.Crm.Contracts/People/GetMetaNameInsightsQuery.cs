using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Query to inspect first-name gender statistics and known nicknames from CRM meta lookups.</summary>
public sealed record GetMetaNameInsightsQuery(string FirstName) : IQuery<MetaNameInsightsResponse>;
