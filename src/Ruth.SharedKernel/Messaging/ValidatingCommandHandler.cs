using Ruth.SharedKernel.Results;

namespace Ruth.SharedKernel.Messaging;

/// <summary>Validation decorator for <see cref="ICommandHandler{TCommand}" />.</summary>
public sealed class ValidatingCommandHandler<TCommand>(
    ICommandHandler<TCommand> inner,
    IEnumerable<IRequestValidator<TCommand>> validators
) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly ICommandHandler<TCommand> _inner = inner
        ?? throw new ArgumentNullException(nameof(inner));

    private readonly IEnumerable<IRequestValidator<TCommand>> _validators = validators
        ?? throw new ArgumentNullException(nameof(validators));

    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = await RequestValidationRunner.ValidateAsync(
            _validators,
            command,
            cancellationToken
        );

        return errors is not null
            ? Result.Failure(errors)
            : await _inner.HandleAsync(command, cancellationToken);
    }
}

/// <summary>Validation decorator for <see cref="ICommandHandler{TCommand, TResponse}" />.</summary>
public sealed class ValidatingCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    IEnumerable<IRequestValidator<TCommand>> validators
) : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly ICommandHandler<TCommand, TResponse> _inner = inner
        ?? throw new ArgumentNullException(nameof(inner));

    private readonly IEnumerable<IRequestValidator<TCommand>> _validators = validators
        ?? throw new ArgumentNullException(nameof(validators));

    /// <inheritdoc />
    public async Task<Result<TResponse>> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = await RequestValidationRunner.ValidateAsync(
            _validators,
            command,
            cancellationToken
        );

        return errors is not null
            ? Result.Failure<TResponse>(errors)
            : await _inner.HandleAsync(command, cancellationToken);
    }
}
