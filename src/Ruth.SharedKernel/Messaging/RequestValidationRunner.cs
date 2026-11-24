using Ruth.SharedKernel.Results;

namespace Ruth.SharedKernel.Messaging;

internal static class RequestValidationRunner
{
    public static async ValueTask<IReadOnlyList<DomainError>?> ValidateAsync<TRequest>(
        IEnumerable<IRequestValidator<TRequest>> validators,
        TRequest request,
        CancellationToken cancellationToken
    )
    {
        List<DomainError>? failures = null;

        foreach (var validator in validators)
        {
            var errors = await validator.ValidateAsync(request, cancellationToken);

            if (errors.Count == 0)
            {
                continue;
            }

            failures ??= [];

            for (var i = 0; i < errors.Count; i++)
            {
                failures.Add(errors[i]);
            }
        }

        return failures;
    }
}
