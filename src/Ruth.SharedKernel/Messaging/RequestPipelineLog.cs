using Microsoft.Extensions.Logging;
using Ruth.SharedKernel.Results;

namespace Ruth.SharedKernel.Messaging;

internal static partial class RequestPipelineLog
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Debug,
        Message = "Executing {RequestKind} {RequestName}."
    )]
    public static partial void Executing(ILogger logger, string requestKind, string requestName);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Debug,
        Message = "Completed {RequestKind} {RequestName} in {ElapsedMilliseconds:F2} ms."
    )]
    public static partial void Succeeded(
        ILogger logger,
        string requestKind,
        string requestName,
        double elapsedMilliseconds
    );

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Warning,
        Message = "{RequestKind} {RequestName} failed with {ErrorType} ({ErrorCode}) in {ElapsedMilliseconds:F2} ms."
    )]
    public static partial void Failed(
        ILogger logger,
        string requestKind,
        string requestName,
        ErrorType errorType,
        string errorCode,
        double elapsedMilliseconds
    );

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Error,
        Message = "Unhandled exception while executing {RequestKind} {RequestName} after {ElapsedMilliseconds:F2} ms."
    )]
    public static partial void UnhandledException(
        ILogger logger,
        Exception exception,
        string requestKind,
        string requestName,
        double elapsedMilliseconds
    );
}
