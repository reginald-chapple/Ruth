using Ruth.SharedKernel.Results;

namespace Ruth.SharedKernel.Messaging;

/// <summary>Handles a write command that produces a <see cref="Result" /> outcome.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>Executes the command.</summary>
    /// <param name="command">The command payload.</param>
    /// <param name="cancellationToken">A token to observe while awaiting completion.</param>
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>Handles a write command that produces a <see cref="Result{TValue}" /> outcome.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The response payload type.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>Executes the command.</summary>
    /// <param name="command">The command payload.</param>
    /// <param name="cancellationToken">A token to observe while awaiting completion.</param>
    Task<Result<TResponse>> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default
    );
}
