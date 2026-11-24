using Ruth.SharedKernel.Jobs;
using Xunit;

namespace Ruth.SharedKernel.Tests.Jobs;

public sealed class JobExecutionResultTests
{
    [Fact]
    public void Success_Creates_Success_Result_With_Optional_Message()
    {
        var withoutMessage = JobExecutionResult.Success();
        var withMessage = JobExecutionResult.Success("Processed 42 records.");

        Assert.Equal(JobExecutionStatus.Success, withoutMessage.Status);
        Assert.Null(withoutMessage.Message);
        Assert.Equal(JobExecutionStatus.Success, withMessage.Status);
        Assert.Equal("Processed 42 records.", withMessage.Message);
    }

    [Fact]
    public void Warning_Creates_Warning_Result_And_Rejects_Blank_Messages()
    {
        var result = JobExecutionResult.Warning("Skipped 2 malformed rows.");

        Assert.Equal(JobExecutionStatus.Warning, result.Status);
        Assert.Equal("Skipped 2 malformed rows.", result.Message);
        _ = Assert.ThrowsAny<ArgumentException>(() => JobExecutionResult.Warning("  "));
    }

    [Fact]
    public void Failed_Creates_Failed_Result_And_Rejects_Blank_Messages()
    {
        var result = JobExecutionResult.Failed("Upstream timeout.");

        Assert.Equal(JobExecutionStatus.Failed, result.Status);
        Assert.Equal("Upstream timeout.", result.Message);
        _ = Assert.ThrowsAny<ArgumentException>(() => JobExecutionResult.Failed(string.Empty));
    }
}
