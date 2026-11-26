using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ruth.Modules.Tenancy.Infrastructure;

/// <summary>Design-time EF Core factory for generating Tenancy platform schema migrations.</summary>
public sealed class TenancyDbContextFactory : IDesignTimeDbContextFactory<TenancyDbContext>
{
    /// <inheritdoc />
    public TenancyDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RuthDatabase")
            ?? "Host=localhost;Port=5432;Database=ruth;Username=ruth;Password=ruth_secret";

        var optionsBuilder = new DbContextOptionsBuilder<TenancyDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString, static npgsql =>
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", TenancyDbContext.SchemaName)
        );

        return new TenancyDbContext(optionsBuilder.Options);
    }
}
