using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ruth.Modules.Communication.Infrastructure;

/// <summary>Design-time EF Core factory for generating Communication module migrations.</summary>
public sealed class CommunicationDbContextFactory
    : IDesignTimeDbContextFactory<CommunicationDbContext>
{
    /// <inheritdoc />
    public CommunicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RuthDatabase")
            ?? "Host=localhost;Port=5432;Database=ruth;Username=ruth;Password=ruth_secret";

        var optionsBuilder = new DbContextOptionsBuilder<CommunicationDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString, static npgsql =>
            _ = npgsql.MigrationsHistoryTable(
                "__EFMigrationsHistory",
                CommunicationDbContext.SchemaName
            )
        );

        return new CommunicationDbContext(optionsBuilder.Options);
    }
}
