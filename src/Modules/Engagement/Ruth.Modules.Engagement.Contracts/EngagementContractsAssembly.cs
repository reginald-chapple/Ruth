using System.Reflection;

namespace Ruth.Modules.Engagement.Contracts;

/// <summary>Assembly marker for the Engagement module contracts assembly.</summary>
public static class EngagementContractsAssembly
{
    /// <summary>Gets the compiled <see cref="Assembly" /> instance for Ruth.Modules.Engagement.Contracts.</summary>
    public static Assembly Instance => typeof(EngagementContractsAssembly).Assembly;
}
