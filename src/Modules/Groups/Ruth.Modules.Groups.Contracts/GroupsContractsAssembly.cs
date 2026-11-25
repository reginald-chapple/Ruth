using System.Reflection;

namespace Ruth.Modules.Groups.Contracts;

/// <summary>Assembly marker for the Groups module contracts.</summary>
public static class GroupsContractsAssembly
{
    /// <summary>Gets the Groups contracts assembly instance.</summary>
    public static Assembly Instance => typeof(GroupsContractsAssembly).Assembly;
}
