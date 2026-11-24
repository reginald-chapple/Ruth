using Microsoft.Extensions.Logging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.SharedKernel.Messaging;

/// <summary>Logging decorator for <see cref="ICommandHandler{TCommand}" />.</summary>
public sealed class LoggingCommandHandler<TCommand>(
    ICommandHandler<TCommand> inner,
    ILogger<LoggingCommandHandler<TCommand>> logger,
    IClock clock
) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private const string RequestKind = "command";
    private static readonly string _requestName = typeof(TCommand).Name;

    private readonly ICommandHandler<TCommand> _inner = inner
        ?? throw new ArgumentNullException(nameof(inner));

    private readonly ILogger<LoggingCommandHandler<TCommand>> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        RequestPipelineLog.Executing(_logger, RequestKind, _requestName);
        var startedAt = _clock.GetTimestamp();

        try
        {
            var result = await _inner.HandleAsync(command, cancellationToken);
            var elapsedMs = _clock.GetElapsedTime(startedAt).TotalMilliseconds;

            if (result.IsSuccess)
            {
                RequestPipelineLog.Succeeded(_logger, RequestKind, _requestName, elapsedMs);
            }
            else
            {
                RequestPipelineLog.Failed(
                    _logger,
                    RequestKind,
                    _requestName,
                    result.PrimaryError.Type,
                    result.PrimaryError.Code,
                    elapsedMs
                );
            }

            return result;
        }
        catch (Exception exception)
        {
            var elapsedMs = _clock.GetElapsedTime(startedAt).TotalMilliseconds;
            RequestPipelineLog.UnhandledException(
                _logger,
                exception,
                RequestKind,
                _requestName,
                elapsedMs
            );
            throw;
        }
    }
}

/// <summary>Logging decorator for <see cref="ICommandHandler{TCommand, TResponse}" />.</summary>
public sealed class LoggingCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    ILogger<LoggingCommandHandler<TCommand, TResponse>> logger,
    IClock clock
) : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private const string RequestKind = "command";
    private static readonly string _requestName = typeof(TCommand).Name;

    private readonly ICommandHandler<TCommand, TResponse> _inner = inner
        ?? throw new ArgumentNullException(nameof(inner));

    private readonly ILogger<LoggingCommandHandler<TCommand, TResponse>> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    /// <inheritdoc />
    public async Task<Result<TResponse>> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        RequestPipelineLog.Executing(_logger, RequestKind, _requestName);
        var startedAt = _clock.GetTimestamp();

        try
        {
            var result = await _inner.HandleAsync(command, cancellationToken);
            var elapsedMs = _clock.GetElapsedTime(startedAt).TotalMilliseconds;

            if (result.IsSuccess)
            {
                RequestPipelineLog.Succeeded(_logger, RequestKind, _requestName, elapsedMs);
            }
            else
            {
                RequestPipelineLog.Failed(
                    _logger,
                    RequestKind,
                    _requestName,
                    result.PrimaryError.Type,
                    result.PrimaryError.Code,
                    elapsedMs
                );
            }

            return result;
        }
        catch (Exception exception)
        {
            var elapsedMs = _clock.GetElapsedTime(startedAt).TotalMilliseconds;
            RequestPipelineLog.UnhandledException(
                _logger,
                exception,
                RequestKind,
                _requestName,
                elapsedMs
            );
            throw;
        }
    }
}
