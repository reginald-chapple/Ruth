using System.Reflection;

namespace Ruth.Modules.CheckIn;

/// <summary>Assembly marker for the CheckIn bounded context implementation.</summary>
public static class CheckInModuleAssembly
{
    /// <summary>Gets the CheckIn module implementation assembly instance.</summary>
    public static Assembly Instance => typeof(CheckInModuleAssembly).Assembly;
}
