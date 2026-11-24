using Ruth.E2E.Tests.Fixtures;

namespace Ruth.E2E.Tests.Tier1;

internal sealed class IsolatedHostScope(
    E2EContainersFixture containers,
    string databaseName,
    string connectionString,
    RuthHostFactory factory
) : IAsyncDisposable
{
    private readonly E2EContainersFixture _containers = containers;
    private bool _disposed;

    public string DatabaseName { get; } = databaseName;

    public string ConnectionString { get; } = connectionString;

    public RuthHostFactory Factory { get; } = factory;

    public DatabaseProbe Probe { get; } = new DatabaseProbe(connectionString);

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            await Factory.DisposeAsync();
        }
        finally
        {
            await _containers.DropDatabaseAsync(DatabaseName);
        }
    }
}
