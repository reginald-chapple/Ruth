using System.Reflection;

namespace Ruth.Modules.Finance;

/// <summary>Assembly marker for the Finance module implementation.</summary>
public static class FinanceModuleAssembly
{
    /// <summary>Gets the compiled <see cref="Assembly" /> for Ruth.Modules.Finance.</summary>
    public static Assembly Instance => typeof(FinanceModuleAssembly).Assembly;
}
