using System.Reflection;

namespace Ruth.Modules.Reporting.Contracts;

/// <summary>Assembly marker for the Reporting module contracts assembly.</summary>
public static class ReportingContractsAssembly
{
    /// <summary>Gets the Reporting contracts assembly reference.</summary>
    public static Assembly Instance => typeof(ReportingContractsAssembly).Assembly;
}
