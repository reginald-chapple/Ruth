using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Analytics;

/// <summary>Returns current row counts and freshness of PostgreSQL star-schema projections.</summary>
public sealed record GetStarSchemaStatusQuery : IQuery<StarSchemaRefreshResponse>;
