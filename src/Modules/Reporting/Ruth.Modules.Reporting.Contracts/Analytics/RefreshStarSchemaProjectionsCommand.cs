using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Analytics;

/// <summary>Refreshes materialized PostgreSQL star-schema dimension/fact tables and BI views.</summary>
public sealed record RefreshStarSchemaProjectionsCommand : ICommand<StarSchemaRefreshResponse>;
