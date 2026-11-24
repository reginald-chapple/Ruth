using Ruth.SharedKernel.Results;
using Xunit;

namespace Ruth.SharedKernel.Tests.Results;

public sealed class DomainErrorAndResultTests
{
    private sealed class InvalidTestResult(bool isSuccess, IReadOnlyList<DomainError> errors)
        : Result(isSuccess, errors);

    [Fact]
    public void DomainError_FactoryMethods_Set_Expected_ErrorType()
    {
        Assert.Equal(ErrorType.Failure, DomainError.Failure("A.1", "Failure").Type);
        Assert.Equal(ErrorType.Validation, DomainError.Validation("A.2", "Validation").Type);
        Assert.Equal(ErrorType.NotFound, DomainError.NotFound("A.3", "NotFound").Type);
        Assert.Equal(ErrorType.Conflict, DomainError.Conflict("A.4", "Conflict").Type);
        Assert.Equal(ErrorType.Unauthorized, DomainError.Unauthorized("A.5", "Unauthorized").Type);
        Assert.Equal(ErrorType.Forbidden, DomainError.Forbidden("A.6", "Forbidden").Type);
        Assert.Equal(ErrorType.Unexpected, DomainError.Unexpected("A.7", "Unexpected").Type);

        _ = Assert.Throws<ArgumentException>(() => DomainError.Failure(" ", "Description"));
        _ = Assert.Throws<ArgumentException>(() => DomainError.Failure("Code", " "));
    }

    [Fact]
    public void Result_Constructor_Enforces_Invariants()
    {
        var error = DomainError.Validation("Test.Invalid", "Invalid input.");

        _ = Assert.Throws<ArgumentNullException>(() =>
            new InvalidTestResult(isSuccess: true, null!)
        );
        _ = Assert.Throws<ArgumentException>(() => new InvalidTestResult(isSuccess: true, [error]));
        _ = Assert.Throws<ArgumentException>(() => new InvalidTestResult(isSuccess: false, []));
        _ = Assert.Throws<ArgumentException>(() =>
            new InvalidTestResult(isSuccess: false, [DomainError.None])
        );
        _ = Assert.Throws<ArgumentException>(() =>
            new InvalidTestResult(isSuccess: false, [null!])
        );
    }

    [Fact]
    public void Result_Success_And_Failure_Operations_Work()
    {
        var success = Result.Success();
        Assert.Same(success, Result.Success());
        Assert.True(success.IsSuccess);
        Assert.False(success.IsFailure);
        Assert.Equal(DomainError.None, success.PrimaryError);
        Assert.Empty(success.Errors);

        var error = DomainError.NotFound("Item.Missing", "Missing item.");
        Result implicitFailure = error;
        var fromError = Result.FromDomainError(error);

        Assert.True(implicitFailure.IsFailure);
        Assert.Equal(error, implicitFailure.PrimaryError);
        _ = Assert.Single(fromError.Errors);

        var tapped = false;
        _ = success.Tap(() => tapped = true);
        Assert.True(tapped);

        var failureTapped = false;
        _ = implicitFailure.Tap(() => failureTapped = true);
        Assert.False(failureTapped);

        Assert.Equal("ok", success.Match(() => "ok", e => e.Code));
        Assert.Equal(error.Code, implicitFailure.Match(() => "ok", e => e.Code));
    }

    [Fact]
    public async Task Result_Bind_And_Combine_Aggregate_Correctly()
    {
        var e1 = DomainError.Validation("Field.One", "One is invalid.");
        var e2 = DomainError.Validation("Field.Two", "Two is invalid.");

        var combinedSuccess = Result.Combine(Result.Success(), Result.Success());
        Assert.True(combinedSuccess.IsSuccess);

        var combinedFailure = Result.Combine(
            Result.Success(),
            Result.Failure(e1),
            Result.Failure(e2)
        );
        Assert.True(combinedFailure.IsFailure);
        Assert.Equal(2, combinedFailure.Errors.Count);
        Assert.Equal(e1, combinedFailure.PrimaryError);

        _ = Assert.Throws<ArgumentException>(() => Result.Combine(Result.Success(), null!));

        Assert.True(Result.Success().Bind(Result.Success).IsSuccess);
        Assert.Equal(42, Result.Success().Bind(() => Result.Success(42)).Value);
        Assert.True(Result.Failure(e1).Bind(Result.Success).IsFailure);
        Assert.True(Result.Failure(e1).Bind(() => Result.Success(42)).IsFailure);

        var asyncOk = await Result.Success().BindAsync(() => Task.FromResult(Result.Success()));
        var asyncFail = await Result.Failure(e1).BindAsync(() => Task.FromResult(Result.Success()));
        var asyncValOk = await Result.Success().BindAsync(() => Task.FromResult(Result.Success(7)));
        var asyncValFail = await Result
            .Failure(e1)
            .BindAsync(() => Task.FromResult(Result.Success(7)));

        Assert.True(asyncOk.IsSuccess);
        Assert.True(asyncFail.IsFailure);
        Assert.Equal(7, asyncValOk.Value);
        Assert.True(asyncValFail.IsFailure);
    }

    [Fact]
    public async Task ResultOfT_Operations_And_Conversions_Work()
    {
        var created = Result.Create("hello");
        var createdNull = Result.Create<string>(value: null);
        Result<string> fromImplicitValue = "world";
        var error = DomainError.Conflict("Item.Duplicate", "Already exists.");
        Result<string> fromImplicitError = error;

        Assert.True(created.TryGetValue(out var value));
        Assert.Equal("hello", value);
        Assert.Equal("hello", created.GetValueOrDefault("fallback"));

        Assert.True(createdNull.IsFailure);
        Assert.Equal(DomainError.NullValue, createdNull.PrimaryError);
        Assert.False(createdNull.TryGetValue(out _));
        Assert.Equal("fallback", createdNull.GetValueOrDefault("fallback"));
        _ = Assert.Throws<InvalidOperationException>(() => _ = createdNull.Value);

        Assert.Equal("WORLD", fromImplicitValue.Map(s => s.ToUpperInvariant()).Value);
        Assert.True(fromImplicitError.Map(s => s.Length).IsFailure);

        Assert.Equal(5, fromImplicitValue.Bind(s => Result.Success(s.Length)).Value);
        Assert.True(fromImplicitError.Bind(s => Result.Success(s.Length)).IsFailure);
        Assert.True(fromImplicitValue.Bind(_ => Result.Success()).IsSuccess);
        Assert.True(fromImplicitError.Bind(_ => Result.Success()).IsFailure);

        var asyncBound = await fromImplicitValue.BindAsync(s =>
            Task.FromResult(Result.Success(s.Length))
        );
        var asyncBoundFail = await fromImplicitError.BindAsync(s =>
            Task.FromResult(Result.Success(s.Length))
        );
        Assert.Equal(5, asyncBound.Value);
        Assert.True(asyncBoundFail.IsFailure);

        var lengthError = DomainError.Validation("Text.Short", "Too short.");
        Assert.True(fromImplicitValue.Ensure(s => s.Length >= 5, lengthError).IsSuccess);
        Assert.Equal(
            lengthError,
            fromImplicitValue.Ensure(s => s.Length > 10, lengthError).PrimaryError
        );
        Assert.Equal(error, fromImplicitError.Ensure(s => s.Length > 0, lengthError).PrimaryError);

        Assert.Equal(5, fromImplicitValue.Match(s => s.Length, _ => -1));
        Assert.Equal(-1, fromImplicitError.Match(s => s.Length, _ => -1));

        string? tappedValue = null;
        _ = fromImplicitValue.Tap(s => tappedValue = s);
        _ = fromImplicitError.Tap(s => tappedValue = s);
        Assert.Equal("world", tappedValue);
    }
}
