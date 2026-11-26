using System.Reflection;

namespace Ruth.Modules.Commerce.Contracts;

/// <summary>Assembly marker for the Commerce module public contracts assembly.</summary>
public static class CommerceContractsAssembly
{
    /// <summary>Gets the compiled <see cref="Assembly" /> instance for Ruth.Modules.Commerce.Contracts.</summary>
    public static Assembly Instance => typeof(CommerceContractsAssembly).Assembly;
}
