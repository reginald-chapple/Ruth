using System.Reflection;

namespace Ruth.Modules.Finance.Contracts;

/// <summary>Assembly marker for the Finance module public contracts.</summary>
public static class FinanceContractsAssembly
{
    /// <summary>Gets the compiled <see cref="Assembly" /> for Ruth.Modules.Finance.Contracts.</summary>
    public static Assembly Instance => typeof(FinanceContractsAssembly).Assembly;
}
