using System.Reflection;

namespace Ruth.Modules.Identity.Contracts;

/// <summary>Provides a compile-time assembly anchor for the Identity module contracts assembly.</summary>
public static class IdentityContractsAssembly
{
    /// <summary>Gets the <see cref="IdentityContractsAssembly" /> assembly instance.</summary>
    public static Assembly Instance => typeof(IdentityContractsAssembly).Assembly;
}
