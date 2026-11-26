using System.Reflection;

namespace Ruth.Modules.Tenancy;

/// <summary>Assembly marker for Ruth.Modules.Tenancy.</summary>
public static class TenancyModuleAssembly
{
    /// <summary>Gets the module assembly instance.</summary>
    public static Assembly Instance => typeof(TenancyModuleAssembly).Assembly;
}
