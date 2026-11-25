using System.Reflection;

namespace Ruth.Modules.CheckIn.Contracts;

/// <summary>Assembly marker for the CheckIn module contracts.</summary>
public static class CheckInContractsAssembly
{
    /// <summary>Gets the CheckIn contracts assembly instance.</summary>
    public static Assembly Instance => typeof(CheckInContractsAssembly).Assembly;
}
