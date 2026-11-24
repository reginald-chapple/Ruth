using Microsoft.Extensions.DependencyInjection;
using Ruth.SharedKernel.Results;

namespace Ruth.SharedKernel.Messaging;

/// <summary>Zero-magic <see cref="IQueryDispatcher" /> resolving handlers directly from DI.</summary>
public sealed class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider
        ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public Task<Result<TResponse>> QueryAsync<TQuery, TResponse>(
        TQuery query,
        CancellationToken cancellationToken = default
    )
        where TQuery : IQuery<TResponse>
    {
        ArgumentNullException.ThrowIfNull(query);

        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        return handler.HandleAsync(query, cancellationToken);
    }
}
