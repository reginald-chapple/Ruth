using Ruth.SharedKernel.Results;

namespace Ruth.SharedKernel.Messaging;

/// <summary>Validates a command or query before its handler executes.</summary>
/// <typeparam name="TRequest">The command or query type to validate.</typeparam>
public interface IRequestValidator<in TRequest>
{
    /// <summary>Validates <paramref name="request" /> and returns any validation errors.</summary>
    /// <param name="request">The incoming command or query.</param>
    /// <param name="cancellationToken">A token to observe while awaiting validation.</param>
    ValueTask<IReadOnlyList<DomainError>> ValidateAsync(
        TRequest request,
        CancellationToken cancellationToken = default
    );
}
