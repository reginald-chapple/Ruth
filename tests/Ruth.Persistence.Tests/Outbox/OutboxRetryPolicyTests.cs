using Ruth.Persistence.Outbox;
using Xunit;

namespace Ruth.Persistence.Tests.Outbox;

public sealed class OutboxRetryPolicyTests
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 8)]
    [InlineData(4, 16)]
    [InlineData(5, 30)]
    [InlineData(40, 30)]
    public void ComputeBackoffDelay_Grows_Exponentially_And_Clamps_To_MaxDelay(
        int attempt,
        int expectedSeconds
    )
    {
        var delay = OutboxRetryPolicy.ComputeBackoffDelay(
            attempt,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(30)
        );

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Fact]
    public void ComputeBackoffDelay_Validates_Bounds()
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            OutboxRetryPolicy.ComputeBackoffDelay(
                0,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(10)
            )
        );
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            OutboxRetryPolicy.ComputeBackoffDelay(
                1,
                TimeSpan.FromSeconds(-1),
                TimeSpan.FromSeconds(10)
            )
        );
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            OutboxRetryPolicy.ComputeBackoffDelay(
                1,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(5)
            )
        );
    }

    [Theory]
    [InlineData(1, 5, false)]
    [InlineData(4, 5, false)]
    [InlineData(5, 5, true)]
    [InlineData(6, 5, true)]
    [InlineData(1, 0, true)]
    public void ShouldDeadLetter_Respects_MaxRetries(
        int nextRetryCount,
        int maxRetries,
        bool expected
    )
    {
        Assert.Equal(expected, OutboxRetryPolicy.ShouldDeadLetter(nextRetryCount, maxRetries));
    }
}
