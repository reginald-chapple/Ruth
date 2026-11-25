using System.Reflection;

namespace Ruth.Modules.Crm.Contracts;

/// <summary>Assembly marker for the CRM module Contracts assembly.</summary>
public static class CrmContractsAssembly
{
    /// <summary>Gets the CRM Contracts assembly instance.</summary>
    public static Assembly Instance => typeof(CrmContractsAssembly).Assembly;
}
