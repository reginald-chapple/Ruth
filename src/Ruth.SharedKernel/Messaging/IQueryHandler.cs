using Ruth.SharedKernel.Results;

namespace Ruth.SharedKernel.Messaging;

/// <summary>Handles a read-only query that produces a <see cref="Result{TValue}" /> outcome.</summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The projection payload type.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>Executes the query.</summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="cancellationToken">A token to observe while awaiting completion.</param>
    Task<Result<TResponse>> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken = default
    );
}
