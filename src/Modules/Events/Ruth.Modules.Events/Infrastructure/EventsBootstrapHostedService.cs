using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Domain.Calendars;

namespace Ruth.Modules.Events.Infrastructure;

internal sealed partial class EventsBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<EventsBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(logger);
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await SeedDefaultCalendarsAsync(dbContext, cancellationToken);
        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task SeedDefaultCalendarsAsync(
        EventsDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        if (await dbContext.EventCalendars.AnyAsync(cancellationToken))
        {
            return;
        }

        EventCalendarSpec[] defaults =
        [
            new(
                Name: "Public Events Calendar",
                Slug: "public-events",
                Description: "Public-facing community, member, and program events.",
                ColorHex: "#0F6CBD",
                IsPublic: true,
                IsIndexEnabled: true,
                IsActive: true
            ),
            new(
                Name: "Internal Operations Calendar",
                Slug: "internal-operations",
                Description: "Staff, volunteer, and facility coordination calendar.",
                ColorHex: "#498205",
                IsPublic: false,
                IsIndexEnabled: false,
                IsActive: true
            ),
        ];

        foreach (var spec in defaults)
        {
            var created = EventCalendar.Create(spec);
            if (created.IsSuccess)
            {
                _ = dbContext.EventCalendars.Add(created.Value);
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(
        EventId = 5701,
        Level = LogLevel.Information,
        Message = "Applying Events schema EF Core migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 5702,
        Level = LogLevel.Information,
        Message = "Events schema migrations completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
