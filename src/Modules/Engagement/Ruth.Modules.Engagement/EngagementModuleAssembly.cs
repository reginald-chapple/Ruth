using System.Reflection;

namespace Ruth.Modules.Engagement;

/// <summary>Assembly marker for the Engagement module implementation assembly.</summary>
public static class EngagementModuleAssembly
{
    /// <summary>Gets the compiled <see cref="Assembly" /> instance for Ruth.Modules.Engagement.</summary>
    public static Assembly Instance => typeof(EngagementModuleAssembly).Assembly;
}
