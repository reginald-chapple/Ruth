using System.Reflection;

namespace Ruth.Modules.Groups;

/// <summary>Assembly marker for the Groups bounded context implementation.</summary>
public static class GroupsModuleAssembly
{
    /// <summary>Gets the Groups module implementation assembly instance.</summary>
    public static Assembly Instance => typeof(GroupsModuleAssembly).Assembly;
}
