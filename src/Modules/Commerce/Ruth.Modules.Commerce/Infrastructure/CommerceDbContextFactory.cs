using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ruth.Modules.Commerce.Infrastructure;

/// <summary>Design-time EF Core factory for generating Commerce module migrations.</summary>
public sealed class CommerceDbContextFactory : IDesignTimeDbContextFactory<CommerceDbContext>
{
    /// <inheritdoc />
    public CommerceDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RuthDatabase")
            ?? "Host=localhost;Port=5432;Database=ruth;Username=ruth;Password=ruth_secret";

        var optionsBuilder = new DbContextOptionsBuilder<CommerceDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString, static npgsql =>
            _ = npgsql.MigrationsHistoryTable("__EFMigrationsHistory", CommerceDbContext.SchemaName)
        );

        return new CommerceDbContext(optionsBuilder.Options);
    }
}
