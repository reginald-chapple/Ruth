using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ruth.Modules.Core.Infrastructure;

/// <summary>Design-time EF Core factory for generating Core module migrations.</summary>
public sealed class CoreDbContextFactory : IDesignTimeDbContextFactory<CoreDbContext>
{
    /// <inheritdoc />
    public CoreDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RuthDatabase")
            ?? "Host=localhost;Port=5432;Database=ruth;Username=ruth;Password=ruth_secret";

        var optionsBuilder = new DbContextOptionsBuilder<CoreDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString, static npgsql =>
        {
            _ = npgsql.MigrationsHistoryTable("__EFMigrationsHistory", CoreDbContext.SchemaName);
            _ = npgsql.UseNetTopologySuite();
        });

        return new CoreDbContext(optionsBuilder.Options);
    }
}
