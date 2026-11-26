using System.Reflection;

namespace Ruth.Modules.Cms.Contracts;

/// <summary>Assembly marker for the Cms module contracts assembly.</summary>
public static class CmsContractsAssembly
{
    /// <summary>Gets the <see cref="CmsContractsAssembly" /> assembly instance.</summary>
    public static Assembly Instance => typeof(CmsContractsAssembly).Assembly;
}
