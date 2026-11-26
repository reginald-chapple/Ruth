using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ruth.Modules.Finance.Infrastructure;

/// <summary>Design-time EF Core factory for generating Finance module migrations.</summary>
public sealed class FinanceDbContextFactory : IDesignTimeDbContextFactory<FinanceDbContext>
{
    /// <inheritdoc />
    public FinanceDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RuthDatabase")
            ?? "Host=localhost;Port=5432;Database=ruth;Username=ruth;Password=ruth_secret";

        var optionsBuilder = new DbContextOptionsBuilder<FinanceDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString, static npgsql =>
            _ = npgsql.MigrationsHistoryTable("__EFMigrationsHistory", FinanceDbContext.SchemaName)
        );

        return new FinanceDbContext(optionsBuilder.Options);
    }
}
