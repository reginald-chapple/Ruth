using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ruth.Modules.Cms.Infrastructure;

/// <summary>Design-time EF Core factory for generating Cms module migrations.</summary>
public sealed class CmsDbContextFactory : IDesignTimeDbContextFactory<CmsDbContext>
{
    /// <inheritdoc />
    public CmsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RuthDatabase")
            ?? "Host=localhost;Port=5432;Database=ruth;Username=ruth;Password=ruth_secret";

        var optionsBuilder = new DbContextOptionsBuilder<CmsDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString, static npgsql =>
            _ = npgsql.MigrationsHistoryTable("__EFMigrationsHistory", CmsDbContext.SchemaName)
        );

        return new CmsDbContext(optionsBuilder.Options);
    }
}
