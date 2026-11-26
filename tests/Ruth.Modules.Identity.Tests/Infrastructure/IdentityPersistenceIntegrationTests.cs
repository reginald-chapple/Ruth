using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.Modules.Identity.Slices.Onboarding;
using Ruth.Modules.Identity.Tests.Fixtures;
using Ruth.Persistence.Outbox;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Xunit;

namespace Ruth.Modules.Identity.Tests.Infrastructure;

[Collection(IdentityPostgresFixtures.Name)]
public sealed class IdentityPersistenceIntegrationTests(Postgres18ContainerFixture postgres)
{
    [Fact]
    public async Task MigrateAndCompleteOnboarding_PersistsTablesOutboxAndDispatchesIntegrationEvent()
    {
        var dbConnection = await postgres.CreateIsolatedDatabaseAsync();
        var consumer = new IdentityTestHostFactory.RecordingOnboardedConsumer();
        await using var provider = IdentityTestHostFactory.BuildServiceProvider(
            dbConnection,
            autoMigrate: true,
            consumer
        );

        await IdentityTestHostFactory.StartBootstrapAsync(provider);
        await ExecuteAndAssertOnboardingAsync(provider);
        await DispatchAndAssertOutboxAsync(provider, consumer);
    }

    [Fact]
    public async Task CompleteOnboarding_ConcurrentSubmissions_AllowsStrictlyOneWinner()
    {
        var dbConnection = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = IdentityTestHostFactory.BuildServiceProvider(
            dbConnection,
            autoMigrate: true
        );
        await IdentityTestHostFactory.StartBootstrapAsync(provider);

        var tasks = Enumerable
            .Range(0, 5)
            .Select(async index =>
            {
                await using var scope = provider.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
                var userName = string.Create(CultureInfo.InvariantCulture, $"admin{index}");
                var email = string.Create(CultureInfo.InvariantCulture, $"admin{index}@grace.org");
                return await dispatcher.SendAsync<
                    CompleteOnboardingCommand,
                    CompleteOnboardingResponse
                >(IdentityTestHostFactory.CreateValidCommand(userName, email));
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);

        _ = Assert.Single(results, static r => r.IsSuccess);
        Assert.Equal(
            4,
            results.Count(static r => r.IsFailure && r.PrimaryError.Type == ErrorType.Conflict)
        );

        await using var verifyScope = provider.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.Equal(1, await db.OrganizationProfiles.CountAsync());
        Assert.Equal(1, await db.UserAccounts.CountAsync());
    }

    [Fact]
    public async Task IdentityDbContextFactory_And_BootstrapDisabled_WorkCleanly()
    {
        var factory = new IdentityDbContextFactory();
        await using var designTimeContext = factory.CreateDbContext([]);
        Assert.NotNull(designTimeContext);

        var dbConnection = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = IdentityTestHostFactory.BuildServiceProvider(
            dbConnection,
            autoMigrate: false
        );

        var bootstrap = provider
            .GetServices<IHostedService>()
            .OfType<IdentityBootstrapHostedService>()
            .Single();
        await bootstrap.StartAsync(CancellationToken.None);
        await bootstrap.StopAsync(CancellationToken.None);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync();
        Assert.NotEmpty(pending);
    }

    [Fact]
    public async Task CompleteOnboardingCommandHandler_RejectsWeakPasswordEvenWithoutValidatorDecorator()
    {
        var dbConnection = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = IdentityTestHostFactory.BuildServiceProvider(
            dbConnection,
            autoMigrate: true
        );
        await IdentityTestHostFactory.StartBootstrapAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<CompleteOnboardingCommandHandler>();
        var weakResult = await handler.HandleAsync(
            IdentityTestHostFactory.CreateValidCommand(password: "weak")
        );

        Assert.True(weakResult.IsFailure);
        Assert.Equal(ErrorType.Validation, weakResult.PrimaryError.Type);
    }

    private static async Task ExecuteAndAssertOnboardingAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var reader = scope.ServiceProvider.GetRequiredService<IOnboardingStateReader>();
        var before = await reader.GetStatusAsync();
        Assert.True(before.IsOnboardingRequired);
        Assert.False(before.IsSelfRegistrationLocked);
        Assert.Null(before.OrganizationName);

        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var result = await dispatcher.SendAsync<
            CompleteOnboardingCommand,
            CompleteOnboardingResponse
        >(IdentityTestHostFactory.CreateValidCommand());
        Assert.True(result.IsSuccess);

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var org = await db.OrganizationProfiles.SingleAsync();
        var role = await db.SecurityRoles.SingleAsync();
        var user = await db.UserAccounts.Include(static u => u.Roles).SingleAsync();
        var outbox = await db.OutboxMessages.SingleAsync();

        Assert.Equal("Civic Foundation", org.Name);
        Assert.Equal("Main Branch", org.PrimaryBranchName);
        Assert.Equal("MAIN", org.PrimaryBranchShortCode);
        Assert.True(org.IsOnboardingCompleted);
        Assert.True(org.SelfRegistrationLocked);
        Assert.Equal(IdentityTestHostFactory.CurrentTenantAdministratorsRoleId, role.Id);
        Assert.Equal("admin", user.UserName);
        Assert.NotEqual("StrongPass#2026!", user.PasswordHash, StringComparer.Ordinal);
        _ = Assert.Single(user.Roles);
        Assert.Contains(
            "OrganizationOnboardedIntegrationEvent",
            outbox.EventType,
            StringComparison.Ordinal
        );

        var after = await reader.GetStatusAsync();
        Assert.False(after.IsOnboardingRequired);
        Assert.True(after.IsSelfRegistrationLocked);
        Assert.Equal("Civic Foundation", after.OrganizationName);

        var cached = await reader.GetStatusAsync();
        Assert.False(cached.IsOnboardingRequired);
        reader.InvalidateCache();
        var reloaded = await reader.GetStatusAsync();
        Assert.False(reloaded.IsOnboardingRequired);
    }

    private static async Task DispatchAndAssertOutboxAsync(
        ServiceProvider provider,
        IdentityTestHostFactory.RecordingOnboardedConsumer consumer
    )
    {
        var outboxDispatcher = provider.GetRequiredService<OutboxDispatcherService>();
        _ = await outboxDispatcher.ProcessPendingOnceAsync();

        var received = Assert.Single(consumer.ReceivedEvents);
        Assert.Equal("Civic Foundation", received.OrganizationName);

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var processedOutbox = await verifyDb.OutboxMessages.SingleAsync();
        _ = Assert.NotNull(processedOutbox.ProcessedOnUtc);
    }
}
