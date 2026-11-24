using Ruth.SharedKernel.Results;

namespace Ruth.SharedKernel.Messaging;

/// <summary>Explicitly dispatches queries to their DI-registered <see cref="IQueryHandler{TQuery, TResponse}" />.</summary>
public interface IQueryDispatcher
{
    /// <summary>Dispatches a query and returns its <see cref="Result{TResponse}" />.</summary>
    Task<Result<TResponse>> QueryAsync<TQuery, TResponse>(
        TQuery query,
        CancellationToken cancellationToken = default
    )
        where TQuery : IQuery<TResponse>;
}
