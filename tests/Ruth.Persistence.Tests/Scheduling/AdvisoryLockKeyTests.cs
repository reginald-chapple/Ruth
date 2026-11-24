using Ruth.Persistence.Scheduling;
using Xunit;

namespace Ruth.Persistence.Tests.Scheduling;

public sealed class AdvisoryLockKeyTests
{
    [Fact]
    public void ComputeAdvisoryLockKey_Is_Deterministic_And_Distinct()
    {
        var keyA1 = PostgresAdvisoryLockProvider.ComputeAdvisoryLockKey("ruth.core.cleanup");
        var keyA2 = PostgresAdvisoryLockProvider.ComputeAdvisoryLockKey("ruth.core.cleanup");
        var keyB = PostgresAdvisoryLockProvider.ComputeAdvisoryLockKey("ruth.core.cleanup2");

        Assert.Equal(keyA1, keyA2);
        Assert.NotEqual(keyA1, keyB);
    }

    [Fact]
    public void ComputeAdvisoryLockKey_Rejects_Blank_Key()
    {
        _ = Assert.ThrowsAny<ArgumentException>(() =>
            PostgresAdvisoryLockProvider.ComputeAdvisoryLockKey(" ")
        );
    }
}
