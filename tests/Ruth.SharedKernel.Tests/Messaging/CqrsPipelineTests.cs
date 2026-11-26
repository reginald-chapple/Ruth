using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Tests.TestDoubles;
using Xunit;

namespace Ruth.SharedKernel.Tests.Messaging;

public sealed class CqrsPipelineTests : IDisposable
{
    private readonly AmbientTenantContext.TenantScope _tenantScope =
        AmbientTenantContext.BeginScope(TenantId.Create());

    public void Dispose()
    {
        _tenantScope.Dispose();
    }

    private sealed record PingCommand(string Value, bool FailDomain = false, bool Throw = false)
        : ICommand;

    private sealed record CreateGreetingCommand(
        string Name,
        bool FailDomain = false,
        bool Throw = false
    ) : ICommand<string>;

    private sealed record LookupGreetingQuery(string Key, bool NotFound = false, bool Throw = false)
        : IQuery<string>;

    private sealed class InvocationTracker
    {
        public int Count { get; set; }
    }

    private sealed class PingCommandHandler(InvocationTracker tracker)
        : ICommandHandler<PingCommand>
    {
        public Task<Result> HandleAsync(
            PingCommand command,
            CancellationToken cancellationToken = default
        )
        {
            tracker.Count++;

            return command.Throw
                ? throw new InvalidOperationException("Ping exploded.")
                : Task.FromResult(
                    command.FailDomain
                        ? Result.Failure(DomainError.Failure("Ping.Failed", "Domain failure."))
                        : Result.Success()
                );
        }
    }

    private sealed class PingNonEmptyValidator : IRequestValidator<PingCommand>
    {
        public ValueTask<IReadOnlyList<DomainError>> ValidateAsync(
            PingCommand request,
            CancellationToken cancellationToken = default
        )
        {
            IReadOnlyList<DomainError> errors = string.IsNullOrWhiteSpace(request.Value)
                ? [DomainError.Validation("Ping.Empty", "Value is required.")]
                : [];
            return ValueTask.FromResult(errors);
        }
    }

    private sealed class PingLengthValidator : IRequestValidator<PingCommand>
    {
        public ValueTask<IReadOnlyList<DomainError>> ValidateAsync(
            PingCommand request,
            CancellationToken cancellationToken = default
        )
        {
            IReadOnlyList<DomainError> errors =
                request.Value.Length < 3
                    ? [DomainError.Validation("Ping.TooShort", "Minimum length is 3.")]
                    : [];
            return ValueTask.FromResult(errors);
        }
    }

    private sealed class CreateGreetingCommandHandler(InvocationTracker tracker)
        : ICommandHandler<CreateGreetingCommand, string>
    {
        public Task<Result<string>> HandleAsync(
            CreateGreetingCommand command,
            CancellationToken cancellationToken = default
        )
        {
            tracker.Count++;

            return command.Throw
                ? throw new InvalidOperationException("Greeting exploded.")
                : Task.FromResult(
                    command.FailDomain
                        ? Result.Failure<string>(
                            DomainError.Conflict("Greeting.Exists", "Duplicate greeting.")
                        )
                        : Result.Success($"Hello, {command.Name}!")
                );
        }
    }

    private sealed class CreateGreetingValidator : IRequestValidator<CreateGreetingCommand>
    {
        public ValueTask<IReadOnlyList<DomainError>> ValidateAsync(
            CreateGreetingCommand request,
            CancellationToken cancellationToken = default
        )
        {
            IReadOnlyList<DomainError> errors = string.IsNullOrWhiteSpace(request.Name)
                ? [DomainError.Validation("Greeting.NameRequired", "Name is required.")]
                : [];
            return ValueTask.FromResult(errors);
        }
    }

    private sealed class LookupGreetingQueryHandler(InvocationTracker tracker)
        : IQueryHandler<LookupGreetingQuery, string>
    {
        public Task<Result<string>> HandleAsync(
            LookupGreetingQuery query,
            CancellationToken cancellationToken = default
        )
        {
            tracker.Count++;

            return query.Throw
                ? throw new InvalidOperationException("Lookup exploded.")
                : Task.FromResult(
                    query.NotFound
                        ? Result.Failure<string>(
                            DomainError.NotFound("Greeting.NotFound", "Not found.")
                        )
                        : Result.Success($"Found:{query.Key}")
                );
        }
    }

    private sealed class LookupGreetingValidator : IRequestValidator<LookupGreetingQuery>
    {
        public ValueTask<IReadOnlyList<DomainError>> ValidateAsync(
            LookupGreetingQuery request,
            CancellationToken cancellationToken = default
        )
        {
            IReadOnlyList<DomainError> errors = string.IsNullOrWhiteSpace(request.Key)
                ? [DomainError.Validation("Lookup.KeyRequired", "Key is required.")]
                : [];
            return ValueTask.FromResult(errors);
        }
    }

    [Fact]
    public async Task VoidCommand_Pipeline_Validates_Logs_And_Dispatches()
    {
        var tracker = new InvocationTracker();
        var logger = new RecordingLogger<LoggingCommandHandler<PingCommand>>();
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero)
        );
        var services = new ServiceCollection();
        _ = services
            .AddSingleton(tracker)
            .AddSingleton<ILogger<LoggingCommandHandler<PingCommand>>>(logger)
            .AddSharedKernel(timeProvider)
            .AddCommandHandler<PingCommand, PingCommandHandler>()
            .AddRequestValidator<PingCommand, PingNonEmptyValidator>()
            .AddRequestValidator<PingCommand, PingLengthValidator>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var invalid = await dispatcher.SendAsync(new PingCommand(string.Empty));
        Assert.True(invalid.IsFailure);
        Assert.Equal(2, invalid.Errors.Count);
        Assert.Equal(0, tracker.Count);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);

        var ok = await dispatcher.SendAsync(new PingCommand("abc"));
        Assert.True(ok.IsSuccess);
        Assert.Equal(1, tracker.Count);

        var failed = await dispatcher.SendAsync(new PingCommand("abc", FailDomain: true));
        Assert.True(failed.IsFailure);
        Assert.Equal("Ping.Failed", failed.PrimaryError.Code);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.SendAsync(new PingCommand("abc", Throw: true))
        );
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Exception is not null);

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.SendAsync((PingCommand)null!)
        );
    }

    [Fact]
    public async Task TypedCommand_Pipeline_Validates_Logs_And_Dispatches()
    {
        var tracker = new InvocationTracker();
        var logger = new RecordingLogger<LoggingCommandHandler<CreateGreetingCommand, string>>();
        var services = new ServiceCollection();
        _ = services
            .AddSingleton(tracker)
            .AddSingleton<ILogger<LoggingCommandHandler<CreateGreetingCommand, string>>>(logger)
            .AddSharedKernel()
            .AddCommandHandler<CreateGreetingCommand, string, CreateGreetingCommandHandler>()
            .AddRequestValidator<CreateGreetingCommand, CreateGreetingValidator>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var invalid = await dispatcher.SendAsync<CreateGreetingCommand, string>(
            new CreateGreetingCommand(" ")
        );
        Assert.True(invalid.IsFailure);
        Assert.Equal(0, tracker.Count);

        var ok = await dispatcher.SendAsync<CreateGreetingCommand, string>(
            new CreateGreetingCommand("Ruth")
        );
        Assert.True(ok.IsSuccess);
        Assert.Equal("Hello, Ruth!", ok.Value);
        Assert.Equal(1, tracker.Count);

        var conflict = await dispatcher.SendAsync<CreateGreetingCommand, string>(
            new CreateGreetingCommand("Ruth", FailDomain: true)
        );
        Assert.Equal(ErrorType.Conflict, conflict.PrimaryError.Type);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.SendAsync<CreateGreetingCommand, string>(
                new CreateGreetingCommand("Ruth", Throw: true)
            )
        );
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Exception is not null);

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.SendAsync<CreateGreetingCommand, string>(null!)
        );
    }

    [Fact]
    public async Task Query_Pipeline_Validates_Logs_And_Dispatches()
    {
        var tracker = new InvocationTracker();
        var logger = new RecordingLogger<LoggingQueryHandler<LookupGreetingQuery, string>>();
        var services = new ServiceCollection();
        _ = services
            .AddSingleton(tracker)
            .AddSingleton<ILogger<LoggingQueryHandler<LookupGreetingQuery, string>>>(logger)
            .AddSharedKernel()
            .AddQueryHandler<LookupGreetingQuery, string, LookupGreetingQueryHandler>()
            .AddRequestValidator<LookupGreetingQuery, LookupGreetingValidator>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var invalid = await dispatcher.QueryAsync<LookupGreetingQuery, string>(
            new LookupGreetingQuery(string.Empty)
        );
        Assert.True(invalid.IsFailure);
        Assert.Equal(0, tracker.Count);

        var ok = await dispatcher.QueryAsync<LookupGreetingQuery, string>(
            new LookupGreetingQuery("42")
        );
        Assert.True(ok.IsSuccess);
        Assert.Equal("Found:42", ok.Value);
        Assert.Equal(1, tracker.Count);

        var missing = await dispatcher.QueryAsync<LookupGreetingQuery, string>(
            new LookupGreetingQuery("42", NotFound: true)
        );
        Assert.Equal(ErrorType.NotFound, missing.PrimaryError.Type);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.QueryAsync<LookupGreetingQuery, string>(
                new LookupGreetingQuery("42", Throw: true)
            )
        );
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Exception is not null);

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.QueryAsync<LookupGreetingQuery, string>(null!)
        );
    }

    [Fact]
    public async Task Pipeline_Throws_TenantContextRequiredException_When_No_Tenant_Is_Active()
    {
        using var unresolved = AmbientTenantContext.BeginUnresolvedScope();
        var services = new ServiceCollection();
        _ = services
            .AddSingleton(new InvocationTracker())
            .AddSingleton<ILogger<LoggingCommandHandler<PingCommand>>>(
                new RecordingLogger<LoggingCommandHandler<PingCommand>>()
            )
            .AddSharedKernel()
            .AddCommandHandler<PingCommand, PingCommandHandler>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        _ = await Assert.ThrowsAsync<TenantContextRequiredException>(() =>
            dispatcher.SendAsync(new PingCommand("abc"))
        );
    }
}
