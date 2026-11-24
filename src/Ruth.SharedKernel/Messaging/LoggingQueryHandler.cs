using Microsoft.Extensions.Logging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.SharedKernel.Messaging;

/// <summary>Logging decorator for <see cref="IQueryHandler{TQuery, TResponse}" />.</summary>
public sealed class LoggingQueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    ILogger<LoggingQueryHandler<TQuery, TResponse>> logger,
    IClock clock
) : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private const string RequestKind = "query";
    private static readonly string _requestName = typeof(TQuery).Name;

    private readonly IQueryHandler<TQuery, TResponse> _inner = inner
        ?? throw new ArgumentNullException(nameof(inner));

    private readonly ILogger<LoggingQueryHandler<TQuery, TResponse>> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    /// <inheritdoc />
    public async Task<Result<TResponse>> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        RequestPipelineLog.Executing(_logger, RequestKind, _requestName);
        var startedAt = _clock.GetTimestamp();

        try
        {
            var result = await _inner.HandleAsync(query, cancellationToken);
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
