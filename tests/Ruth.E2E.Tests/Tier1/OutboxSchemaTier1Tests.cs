using System.Globalization;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier1;

[Collection(E2ETestCollection.Name)]
public sealed class OutboxSchemaTier1Tests(E2EContainersFixture containers)
{
    private static readonly string[] _idColumns = ["Id", "id"];
    private static readonly string[] _typeColumns = ["EventType", "event_type", "Type", "type"];
    private static readonly string[] _payloadColumns =
    [
        "Payload",
        "payload",
        "Content",
        "content",
        "Data",
        "data",
    ];

    [Fact]
    public async Task Feature05_IdentitySchema_ContainsOutboxAndInboxTables()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        var outboxTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "identity",
            ProbeSqlCatalog.OutboxTableCandidates
        );
        var inboxTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "identity",
            ProbeSqlCatalog.InboxTableCandidates
        );

        Assert.NotNull(outboxTable);
        Assert.NotNull(inboxTable);
    }

    [Fact]
    public async Task Feature05_OutboxTable_HasUuidPrimaryKeyAndJsonbPayloadColumn()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        var outboxTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "identity",
            ProbeSqlCatalog.OutboxTableCandidates
        );
        Assert.NotNull(outboxTable);

        var pkColumns = await scope.Probe.GetPrimaryKeyColumnsAsync("identity", outboxTable);
        var columns = await scope.Probe.GetColumnsAsync("identity", outboxTable);

        var pk = Assert.Single(pkColumns);
        var idCol = columns.FirstOrDefault(c =>
            string.Equals(c.ColumnName, pk, StringComparison.OrdinalIgnoreCase)
        );
        var payloadCol = columns.FirstOrDefault(c =>
            string.Equals(c.UdtName, "jsonb", StringComparison.OrdinalIgnoreCase)
        );

        Assert.NotNull(idCol);
        Assert.Equal("uuid", idCol.UdtName);
        Assert.NotNull(payloadCol);
    }

    [Fact]
    public async Task Feature05_OutboxTable_HasPartialIndexForUnprocessedMessages()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        var outboxTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "identity",
            ProbeSqlCatalog.OutboxTableCandidates
        );
        Assert.NotNull(outboxTable);

        var indexes = await scope.Probe.GetIndexesAsync("identity", outboxTable);
        Assert.Contains(indexes, idx =>
            idx.Definition.Contains("WHERE", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public async Task Feature05_InboxTable_HasCompositePrimaryKeyOnMessageIdAndConsumerName()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        var inboxTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "identity",
            ProbeSqlCatalog.InboxTableCandidates
        );
        Assert.NotNull(inboxTable);

        var pkColumns = await scope.Probe.GetPrimaryKeyColumnsAsync("identity", inboxTable);
        Assert.Equal(3, pkColumns.Count);
        Assert.Contains(pkColumns, col =>
            col.Contains("tenant", StringComparison.OrdinalIgnoreCase)
        );
        Assert.Contains(pkColumns, col =>
            col.Contains("message", StringComparison.OrdinalIgnoreCase)
        );
        Assert.Contains(pkColumns, col =>
            col.Contains("consumer", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public async Task Feature05_Onboarding_CapturesOrganizationOnboardedOutboxMessageWithUuidV7()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        using var doc = await Tier1TestSupport.CompleteOnboardingAsync(
            client,
            TestOnboardingPayloads.CreateValidPayload(organizationName: "Outbox Test Parish")
        );
        var orgId = doc.RootElement.GetProperty("organizationId").GetGuid();

        var outboxRows = await scope.Probe.ReadIdentityOutboxMessagesAsync();
        var onboardedRow = outboxRows.FirstOrDefault(row =>
            Tier1TestSupport.TryGetAnyColumn(row, _typeColumns, out var typeRaw)
            && (Convert.ToString(typeRaw, CultureInfo.InvariantCulture) ?? string.Empty).Contains(
                "OrganizationOnboarded",
                StringComparison.OrdinalIgnoreCase
            )
        );
        Assert.NotNull(onboardedRow);

        Assert.True(Tier1TestSupport.TryGetAnyColumn(onboardedRow, _idColumns, out var idRaw));
        TestOnboardingPayloads.AssertIsUuidV7(Assert.IsType<Guid>(idRaw));

        Assert.True(
            Tier1TestSupport.TryGetAnyColumn(onboardedRow, _payloadColumns, out var payloadRaw)
        );
        var payloadJson = Convert.ToString(payloadRaw, CultureInfo.InvariantCulture)
            ?? string.Empty;
        Assert.Contains("Outbox Test Parish", payloadJson, StringComparison.Ordinal);
        Assert.Contains(
            orgId.ToString("D", CultureInfo.InvariantCulture),
            payloadJson,
            StringComparison.OrdinalIgnoreCase
        );
    }
}
