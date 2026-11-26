using System.Reflection;

namespace Ruth.Modules.Search.Contracts;

/// <summary>Assembly marker for the Search module contracts assembly.</summary>
public static class SearchContractsAssembly
{
    /// <summary>Gets the Search contracts assembly reference.</summary>
    public static Assembly Instance => typeof(SearchContractsAssembly).Assembly;
}
