using Microsoft.Extensions.Logging;

namespace Ruth.Persistence.EntityFramework;

internal static partial class EntityFrameworkLog
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Debug,
        Message = "Dispatching {EventCount} committed domain event(s) for {DbContextType}."
    )]
    public static partial void DrainedDomainEvents(
        ILogger logger,
        int eventCount,
        string dbContextType
    );

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Debug,
        Message = "Discarded uncommitted staged domain events after SaveChanges failure on {DbContextType}."
    )]
    public static partial void DiscardedDomainEvents(ILogger logger, string dbContextType);
}
