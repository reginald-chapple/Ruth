using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ruth.Modules.Identity.Infrastructure;

/// <summary>Design-time EF Core factory for generating Identity module migrations.</summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    /// <inheritdoc />
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RuthDatabase")
            ?? "Host=localhost;Port=5432;Database=ruth;Username=ruth;Password=ruth_secret";

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString, static npgsql =>
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.SchemaName)
        );

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
