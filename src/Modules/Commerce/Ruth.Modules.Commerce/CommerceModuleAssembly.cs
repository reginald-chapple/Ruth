using System.Reflection;

namespace Ruth.Modules.Commerce;

/// <summary>Assembly marker for the Commerce module implementation assembly.</summary>
public static class CommerceModuleAssembly
{
    /// <summary>Gets the compiled <see cref="Assembly" /> instance for Ruth.Modules.Commerce.</summary>
    public static Assembly Instance => typeof(CommerceModuleAssembly).Assembly;
}
