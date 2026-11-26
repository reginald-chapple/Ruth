using Ruth.Modules.Reporting.Contracts.DataViews;

namespace Ruth.Modules.Reporting.Tests.Fixtures;

internal sealed class StubPersonSourceProvider(IReadOnlyList<DataViewEntityRecord> records)
    : IDataViewSourceProvider
{
    public string EntityTypeKey => "Ruth.Crm.Person";
    public string DisplayName => "Stub People";
    public IReadOnlyList<string> AvailableFields => ["FirstName", "LastName", "RecordStatus"];

    public Task<IReadOnlyList<DataViewEntityRecord>> LoadRecordsAsync(
        int maxRows,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult<IReadOnlyList<DataViewEntityRecord>>([.. records.Take(maxRows)]);
    }

    public static StubPersonSourceProvider CreateDefault(Guid aliceId, Guid bobId)
    {
        return new StubPersonSourceProvider([
            new DataViewEntityRecord(
                aliceId,
                "Ruth.Crm.Person",
                aliceId,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["FirstName"] = "Alice",
                    ["LastName"] = "Rivera",
                    ["RecordStatus"] = "Active",
                    ["Email"] = "alice@example.org",
                },
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["tier"] = "Gold",
                }
            ),
            new DataViewEntityRecord(
                bobId,
                "Ruth.Crm.Person",
                bobId,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["FirstName"] = "Bob",
                    ["LastName"] = "Stone",
                    ["RecordStatus"] = "Inactive",
                    ["Email"] = "bob@example.org",
                },
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["tier"] = "Silver",
                }
            ),
        ]);
    }
}
