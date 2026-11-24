using System.Diagnostics.CodeAnalysis;

namespace Ruth.SharedKernel.Results;

/// <summary>Represents the outcome of a domain or application operation without a payload.</summary>
public class Result : IResult
{
    private static readonly Result _successSingleton = new(isSuccess: true, []);

    /// <summary>Initializes a new <see cref="Result" /> instance after validating invariants.</summary>
    protected Result(bool isSuccess, IReadOnlyList<DomainError> errors)
    {
        ValidateConstructorArguments(isSuccess, errors);

        IsSuccess = isSuccess;
        Errors = isSuccess ? [] : CopyErrors(errors);
        PrimaryError = isSuccess ? DomainError.None : Errors[0];
    }

    /// <inheritdoc />
    public bool IsSuccess { get; }

    /// <inheritdoc />
    public bool IsFailure => !IsSuccess;

    /// <inheritdoc />
    public DomainError PrimaryError { get; }

    /// <inheritdoc />
    public IReadOnlyList<DomainError> Errors { get; }

    /// <summary>Returns a cached successful <see cref="Result" />.</summary>
    public static Result Success()
    {
        return _successSingleton;
    }

    /// <summary>Creates a successful <see cref="Result{TValue}" /> containing <paramref name="value" />.</summary>
    public static Result<TValue> Success<TValue>(TValue value)
    {
        return new Result<TValue>(value);
    }

    /// <summary>Creates a failed <see cref="Result" /> from a single <see cref="DomainError" />.</summary>
    public static Result Failure(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(isSuccess: false, [error]);
    }

    /// <summary>Creates a failed <see cref="Result" /> from one or more errors.</summary>
    public static Result Failure(IReadOnlyList<DomainError> errors)
    {
        return new Result(isSuccess: false, errors);
    }

    /// <summary>Creates a failed <see cref="Result{TValue}" /> from a single <see cref="DomainError" />.</summary>
    public static Result<TValue> Failure<TValue>(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<TValue>([error]);
    }

    /// <summary>Creates a failed <see cref="Result{TValue}" /> from one or more errors.</summary>
    public static Result<TValue> Failure<TValue>(IReadOnlyList<DomainError> errors)
    {
        return new Result<TValue>(errors);
    }

    /// <summary>Creates a <see cref="Result{TValue}" />, failing with <see cref="DomainError.NullValue" /> when null.</summary>
    public static Result<TValue> Create<TValue>(TValue? value)
    {
        return value is not null ? Success(value) : Failure<TValue>(DomainError.NullValue);
    }

    /// <summary>Combines multiple results into a single success or aggregated failure.</summary>
    public static Result Combine(params ReadOnlySpan<Result> results)
    {
        List<DomainError>? failures = null;

        foreach (var result in results)
        {
            if (result is null)
            {
                throw new ArgumentException(
                    "Results cannot contain null entries.",
                    nameof(results)
                );
            }

            if (result.IsFailure)
            {
                failures ??= [];
                failures.AddRange(result.Errors);
            }
        }

        return failures is null ? Success() : Failure(failures);
    }

    /// <summary>Projects this result into a value by invoking the matching continuation.</summary>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<DomainError, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess() : onFailure(PrimaryError);
    }

    /// <summary>Chains another operation when this result succeeded.</summary>
    public Result Bind(Func<Result> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return IsSuccess ? next() : this;
    }

    /// <summary>Chains a value-producing operation when this result succeeded.</summary>
    public Result<TNext> Bind<TNext>(Func<Result<TNext>> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return IsSuccess ? next() : Failure<TNext>(Errors);
    }

    /// <summary>Asynchronously chains another operation when this result succeeded.</summary>
    public Task<Result> BindAsync(Func<Task<Result>> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return IsSuccess ? next() : Task.FromResult(this);
    }

    /// <summary>Asynchronously chains a value-producing operation when this result succeeded.</summary>
    public Task<Result<TNext>> BindAsync<TNext>(Func<Task<Result<TNext>>> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return IsSuccess ? next() : Task.FromResult(Failure<TNext>(Errors));
    }

    /// <summary>Executes a side effect when this result succeeded and returns itself.</summary>
    public Result Tap(Action onSuccess)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (IsSuccess)
        {
            onSuccess();
        }

        return this;
    }

    /// <summary>Converts a <see cref="DomainError" /> into a failed <see cref="Result" />.</summary>
    public static Result FromDomainError(DomainError error)
    {
        return Failure(error);
    }

    /// <summary>Implicitly converts a <see cref="DomainError" /> into a failed <see cref="Result" />.</summary>
    public static implicit operator Result(DomainError error)
    {
        return Failure(error);
    }

    private static void ValidateConstructorArguments(
        bool isSuccess,
        IReadOnlyList<DomainError> errors
    )
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (isSuccess)
        {
            if (errors.Count > 0)
            {
                throw new ArgumentException(
                    "A successful result cannot contain errors.",
                    nameof(errors)
                );
            }

            return;
        }

        if (errors.Count == 0)
        {
            throw new ArgumentException(
                "A failed result must contain at least one error.",
                nameof(errors)
            );
        }

        if (HasSentinelError(errors))
        {
            throw new ArgumentException(
                "A failed result cannot contain DomainError.None.",
                nameof(errors)
            );
        }
    }

    private static bool HasSentinelError(IReadOnlyList<DomainError> errors)
    {
        for (var i = 0; i < errors.Count; i++)
        {
            if (errors[i] is null || errors[i] == DomainError.None)
            {
                return true;
            }
        }

        return false;
    }

    private static DomainError[] CopyErrors(IReadOnlyList<DomainError> errors)
    {
        var copy = new DomainError[errors.Count];

        for (var i = 0; i < errors.Count; i++)
        {
            copy[i] = errors[i];
        }

        return copy;
    }
}

/// <summary>Represents the outcome of a domain or application operation carrying <typeparamref name="TValue" />.</summary>
/// <typeparam name="TValue">The payload type returned on success.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue value)
        : base(isSuccess: true, [])
    {
        _value = value;
    }

    internal Result(IReadOnlyList<DomainError> errors)
        : base(isSuccess: false, errors)
    {
        _value = default;
    }

    /// <summary>Gets the payload value when <see cref="Result.IsSuccess" /> is <see langword="true" />.</summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed on a failed result.</exception>
    public TValue Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException(
                $"Cannot access Value on a failed Result ('{PrimaryError.Code}')."
            );

    /// <summary>Attempts to read the payload value without throwing on failure.</summary>
    public bool TryGetValue([MaybeNullWhen(false)] out TValue value)
    {
        if (IsSuccess)
        {
            value = _value!;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Returns the payload value when successful, otherwise <paramref name="defaultValue" />.</summary>
    public TValue GetValueOrDefault(TValue defaultValue)
    {
        return IsSuccess ? _value! : defaultValue;
    }

    /// <summary>Transforms the payload value when successful, preserving errors on failure.</summary>
    public Result<TOut> Map<TOut>(Func<TValue, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return IsSuccess ? Success(mapper(_value!)) : Failure<TOut>(Errors);
    }

    /// <summary>Chains another value-producing operation when successful.</summary>
    public Result<TOut> Bind<TOut>(Func<TValue, Result<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return IsSuccess ? binder(_value!) : Failure<TOut>(Errors);
    }

    /// <summary>Chains a non-generic operation when successful.</summary>
    public Result Bind(Func<TValue, Result> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return IsSuccess ? binder(_value!) : Failure(Errors);
    }

    /// <summary>Asynchronously chains another value-producing operation when successful.</summary>
    public Task<Result<TOut>> BindAsync<TOut>(Func<TValue, Task<Result<TOut>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return IsSuccess ? binder(_value!) : Task.FromResult(Failure<TOut>(Errors));
    }

    /// <summary>Validates a predicate against the payload value, failing with <paramref name="error" /> if false.</summary>
    public Result<TValue> Ensure(Func<TValue, bool> predicate, DomainError error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);

        return IsFailure || predicate(_value!) ? this : Failure<TValue>(error);
    }

    /// <summary>Projects this result into a value by invoking the matching continuation.</summary>
    public TOut Match<TOut>(Func<TValue, TOut> onSuccess, Func<DomainError, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess(_value!) : onFailure(PrimaryError);
    }

    /// <summary>Executes a side effect with the payload when successful and returns itself.</summary>
    public Result<TValue> Tap(Action<TValue> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (IsSuccess)
        {
            onSuccess(_value!);
        }

        return this;
    }

    /// <summary>Implicitly converts a value into a <see cref="Result{TValue}" /> via <see cref="Result.Create{TValue}" />.</summary>
    public static implicit operator Result<TValue>(TValue? value)
    {
        return Create(value);
    }

    /// <summary>Implicitly converts a <see cref="DomainError" /> into a failed <see cref="Result{TValue}" />.</summary>
    public static implicit operator Result<TValue>(DomainError error)
    {
        return Failure<TValue>(error);
    }
}
