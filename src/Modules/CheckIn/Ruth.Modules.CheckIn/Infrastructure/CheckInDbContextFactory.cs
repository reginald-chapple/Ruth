using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ruth.Modules.CheckIn.Infrastructure;

/// <summary>Design-time EF Core factory for generating CheckIn module migrations.</summary>
public sealed class CheckInDbContextFactory : IDesignTimeDbContextFactory<CheckInDbContext>
{
    /// <inheritdoc />
    public CheckInDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RuthDatabase")
            ?? "Host=localhost;Port=5432;Database=ruth;Username=ruth;Password=ruth_secret";

        var optionsBuilder = new DbContextOptionsBuilder<CheckInDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString, static npgsql =>
            _ = npgsql.MigrationsHistoryTable("__EFMigrationsHistory", CheckInDbContext.SchemaName)
        );

        return new CheckInDbContext(optionsBuilder.Options);
    }
}
