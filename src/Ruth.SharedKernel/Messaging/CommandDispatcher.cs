using Microsoft.Extensions.DependencyInjection;
using Ruth.SharedKernel.Results;

namespace Ruth.SharedKernel.Messaging;

/// <summary>Zero-magic <see cref="ICommandDispatcher" /> resolving handlers directly from DI.</summary>
public sealed class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider
        ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public Task<Result> SendAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default
    )
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
        return handler.HandleAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<TResponse>> SendAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default
    )
        where TCommand : ICommand<TResponse>
    {
        ArgumentNullException.ThrowIfNull(command);

        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return handler.HandleAsync(command, cancellationToken);
    }
}
