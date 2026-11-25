using System.Reflection;

namespace Ruth.Modules.Core;

/// <summary>Assembly marker for the Core module implementation assembly.</summary>
public static class CoreModuleAssembly
{
    /// <summary>Gets the Core module assembly instance.</summary>
    public static Assembly Instance => typeof(CoreModuleAssembly).Assembly;
}
