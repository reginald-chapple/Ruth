namespace Ruth.E2E.Tests.Fixtures;

public static class DatabaseProbeExtensions
{
    public static Task<long> CountIdentityOutboxMessagesAsync(
        this DatabaseProbe probe,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(probe);
        return TableQueryReader.CountFirstExistingAsync(
            probe.ConnectionString,
            "identity",
            ProbeSqlCatalog.OutboxTableCandidates,
            ct
        );
    }

    public static Task<
        IReadOnlyList<IReadOnlyDictionary<string, object?>>
    > ReadIdentityOutboxMessagesAsync(
        this DatabaseProbe probe,
        int limit = 100,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(probe);
        return TableQueryReader.ReadFirstExistingAsync(
            probe.ConnectionString,
            "identity",
            ProbeSqlCatalog.OutboxTableCandidates,
            limit,
            ct
        );
    }

    public static Task<long> CountIdentityInboxMessagesAsync(
        this DatabaseProbe probe,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(probe);
        return TableQueryReader.CountFirstExistingAsync(
            probe.ConnectionString,
            "identity",
            ProbeSqlCatalog.InboxTableCandidates,
            ct
        );
    }

    public static Task<
        IReadOnlyList<IReadOnlyDictionary<string, object?>>
    > ReadIdentityInboxMessagesAsync(
        this DatabaseProbe probe,
        int limit = 100,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(probe);
        return TableQueryReader.ReadFirstExistingAsync(
            probe.ConnectionString,
            "identity",
            ProbeSqlCatalog.InboxTableCandidates,
            limit,
            ct
        );
    }

    public static Task<long> CountUserAccountsAsync(
        this DatabaseProbe probe,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(probe);
        return TableQueryReader.CountFirstExistingAsync(
            probe.ConnectionString,
            "identity",
            ProbeSqlCatalog.UserAccountTableCandidates,
            ct
        );
    }

    public static Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadUserAccountsAsync(
        this DatabaseProbe probe,
        int limit = 100,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(probe);
        return TableQueryReader.ReadFirstExistingAsync(
            probe.ConnectionString,
            "identity",
            ProbeSqlCatalog.UserAccountTableCandidates,
            limit,
            ct
        );
    }

    public static Task<long> CountOrganizationProfilesAsync(
        this DatabaseProbe probe,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(probe);
        return TableQueryReader.CountFirstExistingAsync(
            probe.ConnectionString,
            "identity",
            ProbeSqlCatalog.OrganizationTableCandidates,
            ct
        );
    }

    public static Task<
        IReadOnlyList<IReadOnlyDictionary<string, object?>>
    > ReadOrganizationProfilesAsync(
        this DatabaseProbe probe,
        int limit = 100,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(probe);
        return TableQueryReader.ReadFirstExistingAsync(
            probe.ConnectionString,
            "identity",
            ProbeSqlCatalog.OrganizationTableCandidates,
            limit,
            ct
        );
    }

    public static Task<long> CountScheduledJobsAsync(
        this DatabaseProbe probe,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(probe);
        return TableQueryReader.CountFirstExistingAsync(
            probe.ConnectionString,
            "scheduler",
            ProbeSqlCatalog.ScheduledJobTableCandidates,
            ct
        );
    }

    public static Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadScheduledJobsAsync(
        this DatabaseProbe probe,
        int limit = 100,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(probe);
        return TableQueryReader.ReadFirstExistingAsync(
            probe.ConnectionString,
            "scheduler",
            ProbeSqlCatalog.ScheduledJobTableCandidates,
            limit,
            ct
        );
    }

    public static Task<long> CountScheduledJobHistoryAsync(
        this DatabaseProbe probe,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(probe);
        return TableQueryReader.CountFirstExistingAsync(
            probe.ConnectionString,
            "scheduler",
            ProbeSqlCatalog.ScheduledJobHistoryTableCandidates,
            ct
        );
    }

    public static Task<
        IReadOnlyList<IReadOnlyDictionary<string, object?>>
    > ReadScheduledJobHistoryAsync(
        this DatabaseProbe probe,
        int limit = 100,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(probe);
        return TableQueryReader.ReadFirstExistingAsync(
            probe.ConnectionString,
            "scheduler",
            ProbeSqlCatalog.ScheduledJobHistoryTableCandidates,
            limit,
            ct
        );
    }
}
