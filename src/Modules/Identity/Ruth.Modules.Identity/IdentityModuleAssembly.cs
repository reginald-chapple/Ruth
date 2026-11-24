using System.Reflection;

namespace Ruth.Modules.Identity;

/// <summary>Provides a compile-time assembly anchor for the Identity module implementation.</summary>
public static class IdentityModuleAssembly
{
    /// <summary>Gets the <see cref="IdentityModuleAssembly" /> assembly instance.</summary>
    public static Assembly Instance => typeof(IdentityModuleAssembly).Assembly;
}
