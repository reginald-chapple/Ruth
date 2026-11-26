using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ruth.Modules.Events.Infrastructure;

/// <summary>Design-time EF Core factory for generating Events module migrations.</summary>
public sealed class EventsDbContextFactory : IDesignTimeDbContextFactory<EventsDbContext>
{
    public EventsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RuthDatabase")
            ?? "Host=localhost;Port=5432;Database=ruth;Username=ruth;Password=ruth_secret";

        var optionsBuilder = new DbContextOptionsBuilder<EventsDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString, static npgsql =>
            _ = npgsql.MigrationsHistoryTable("__EFMigrationsHistory", EventsDbContext.SchemaName)
        );

        return new EventsDbContext(optionsBuilder.Options);
    }
}
