using System.Reflection;

namespace Ruth.Modules.Crm;

/// <summary>Assembly marker for the CRM module implementation assembly.</summary>
public static class CrmModuleAssembly
{
    /// <summary>Gets the CRM module assembly instance.</summary>
    public static Assembly Instance => typeof(CrmModuleAssembly).Assembly;
}
