using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Persistence.Tests.Fixtures;

internal static class TestSchemaHelper
{
    public static async Task EnsureSchemaTablesCreatedAsync(DbContext dbContext, string schema)
    {
        var validated = SchemaValidator.Validate(schema);
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await dbContext.Database.OpenConnectionAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{validated}\";";
            _ = await command.ExecuteNonQueryAsync();
        }
        finally
        {
            if (shouldClose)
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        var creator = dbContext.GetService<IRelationalDatabaseCreator>();
        await creator.CreateTablesAsync();
    }
}
