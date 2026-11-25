using System.Reflection;

namespace Ruth.Modules.Core.Contracts;

/// <summary>Assembly marker for the Core module Contracts assembly.</summary>
public static class CoreContractsAssembly
{
    /// <summary>Gets the Core Contracts assembly instance.</summary>
    public static Assembly Instance => typeof(CoreContractsAssembly).Assembly;
}
