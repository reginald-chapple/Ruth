namespace Ruth.SharedKernel.Messaging;

/// <summary>Represents a read-only query that produces a <see cref="Results.Result{TValue}" /> outcome.</summary>
/// <typeparam name="TResponse">The projection payload type returned when the query succeeds.</typeparam>
public interface IQuery<TResponse>;
