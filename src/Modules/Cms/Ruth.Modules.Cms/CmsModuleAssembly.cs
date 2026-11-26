using System.Reflection;

namespace Ruth.Modules.Cms;

/// <summary>Assembly marker for the Cms module implementation assembly.</summary>
public static class CmsModuleAssembly
{
    /// <summary>Gets the <see cref="Cms" /> assembly instance.</summary>
    public static Assembly Instance => typeof(CmsModuleAssembly).Assembly;
}
