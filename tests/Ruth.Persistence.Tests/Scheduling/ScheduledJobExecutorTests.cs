using Ruth.Persistence.Scheduling;
using Xunit;

namespace Ruth.Persistence.Tests.Scheduling;

public sealed class ScheduledJobExecutorTests
{
    [Fact]
    public void FormatExceptionMessage_Handles_Standard_And_Aggregate_Exceptions()
    {
        var singleInner = new AggregateException(new InvalidOperationException("Single inner"));
        var multiInner = new AggregateException(
            new TimeoutException("First timeout"),
            new InvalidOperationException("Second")
        );
        var plain = new InvalidOperationException("Bad op");

        Assert.Equal(
            "InvalidOperationException: Single inner",
            ScheduledJobExecutor.FormatExceptionMessage(singleInner)
        );
        Assert.Contains(
            "One or more exceptions occurred. First Exception: TimeoutException: First timeout",
            ScheduledJobExecutor.FormatExceptionMessage(multiInner),
            StringComparison.Ordinal
        );
        Assert.Equal(
            "InvalidOperationException: Bad op",
            ScheduledJobExecutor.FormatExceptionMessage(plain)
        );
    }
}
