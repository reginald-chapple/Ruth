using System.Reflection;

namespace Ruth.Modules.Reporting;

/// <summary>Assembly marker for the Reporting module implementation assembly.</summary>
public static class ReportingModuleAssembly
{
    public static Assembly Instance => typeof(ReportingModuleAssembly).Assembly;
}
