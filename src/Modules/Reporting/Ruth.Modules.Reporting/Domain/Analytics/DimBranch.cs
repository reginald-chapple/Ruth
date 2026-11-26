namespace Ruth.Modules.Reporting.Domain.Analytics;

/// <summary>Star-schema Branch operating-unit dimension table (reporting.dim_branch).</summary>
public sealed class DimBranch
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string? ShortCode { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset RefreshedAtUtc { get; set; }
}
