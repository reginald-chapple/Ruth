namespace Ruth.SharedKernel.Messaging;

/// <summary>Represents a write command that produces a <see cref="Results.Result" /> outcome.</summary>
public interface ICommand;

/// <summary>Represents a write command that produces a <see cref="Results.Result{TValue}" /> outcome.</summary>
/// <typeparam name="TResponse">The payload type returned when the command succeeds.</typeparam>
public interface ICommand<TResponse>;
