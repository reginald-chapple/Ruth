namespace Ruth.Host;

internal static partial class HostLog
{
    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Warning,
        Message = "Handled client exception ({StatusCode} {Title}) on {Method} {Path}."
    )]
    public static partial void ClientExceptionHandled(
        ILogger logger,
        Exception exception,
        int statusCode,
        string title,
        string method,
        string path
    );

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Error,
        Message = "Unhandled exception on {Method} {Path} mapped to {StatusCode}."
    )]
    public static partial void UnhandledException(
        ILogger logger,
        Exception exception,
        int statusCode,
        string method,
        string path
    );

    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Warning,
        Message = "Rate limit exceeded for {Method} {Path} from partition '{PartitionKey}'."
    )]
    public static partial void RateLimitExceeded(
        ILogger logger,
        string method,
        string path,
        string partitionKey
    );

    [LoggerMessage(
        EventId = 5004,
        Level = LogLevel.Warning,
        Message = "Readiness health check '{CheckName}' failed."
    )]
    public static partial void HealthCheckFailed(
        ILogger logger,
        Exception? exception,
        string checkName
    );
}
