using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ruth.Modules.Groups.Infrastructure;

/// <summary>Design-time EF Core factory for generating Groups module migrations.</summary>
public sealed class GroupsDbContextFactory : IDesignTimeDbContextFactory<GroupsDbContext>
{
    /// <inheritdoc />
    public GroupsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RuthDatabase")
            ?? "Host=localhost;Port=5432;Database=ruth;Username=ruth;Password=ruth_secret";

        var optionsBuilder = new DbContextOptionsBuilder<GroupsDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString, static npgsql =>
        {
            _ = npgsql.UseNetTopologySuite();
            _ = npgsql.MigrationsHistoryTable("__EFMigrationsHistory", GroupsDbContext.SchemaName);
        });

        return new GroupsDbContext(optionsBuilder.Options);
    }
}
