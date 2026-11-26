using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ruth.Modules.VirtualRooms.Infrastructure;

/// <summary>Design-time EF Core factory for generating VirtualRooms module migrations.</summary>
public sealed class VirtualRoomsDbContextFactory
    : IDesignTimeDbContextFactory<VirtualRoomsDbContext>
{
    /// <inheritdoc />
    public VirtualRoomsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RuthDatabase")
            ?? "Host=localhost;Port=5432;Database=ruth;Username=ruth;Password=ruth_secret";

        var optionsBuilder = new DbContextOptionsBuilder<VirtualRoomsDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString, static npgsql =>
            _ = npgsql.MigrationsHistoryTable(
                "__EFMigrationsHistory",
                VirtualRoomsDbContext.SchemaName
            )
        );

        return new VirtualRoomsDbContext(optionsBuilder.Options);
    }
}
