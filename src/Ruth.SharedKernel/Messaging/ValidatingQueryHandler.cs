using Ruth.SharedKernel.Results;

namespace Ruth.SharedKernel.Messaging;

/// <summary>Validation decorator for <see cref="IQueryHandler{TQuery, TResponse}" />.</summary>
public sealed class ValidatingQueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    IEnumerable<IRequestValidator<TQuery>> validators
) : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private readonly IQueryHandler<TQuery, TResponse> _inner = inner
        ?? throw new ArgumentNullException(nameof(inner));

    private readonly IEnumerable<IRequestValidator<TQuery>> _validators = validators
        ?? throw new ArgumentNullException(nameof(validators));

    /// <inheritdoc />
    public async Task<Result<TResponse>> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var errors = await RequestValidationRunner.ValidateAsync(
            _validators,
            query,
            cancellationToken
        );

        return errors is not null
            ? Result.Failure<TResponse>(errors)
            : await _inner.HandleAsync(query, cancellationToken);
    }
}
