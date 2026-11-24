using Ruth.SharedKernel.Results;

namespace Ruth.SharedKernel.Messaging;

/// <summary>Explicitly dispatches commands to their DI-registered <see cref="ICommandHandler{TCommand}" />.</summary>
public interface ICommandDispatcher
{
    /// <summary>Dispatches a command that returns a <see cref="Result" />.</summary>
    Task<Result> SendAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default
    )
        where TCommand : ICommand;

    /// <summary>Dispatches a command that returns a <see cref="Result{TResponse}" />.</summary>
    Task<Result<TResponse>> SendAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default
    )
        where TCommand : ICommand<TResponse>;
}
