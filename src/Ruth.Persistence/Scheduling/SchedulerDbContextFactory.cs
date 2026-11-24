using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ruth.Persistence.Scheduling;

/// <summary>Design-time EF Core factory for generating Scheduler migrations.</summary>
public sealed class SchedulerDbContextFactory : IDesignTimeDbContextFactory<SchedulerDbContext>
{
    /// <inheritdoc />
    public SchedulerDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RuthDatabase")
            ?? "Host=localhost;Port=5432;Database=ruth;Username=ruth;Password=ruth_secret";

        var optionsBuilder = new DbContextOptionsBuilder<SchedulerDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString, static npgsql =>
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", SchedulerDbContext.SchemaName)
        );

        return new SchedulerDbContext(optionsBuilder.Options);
    }
}
