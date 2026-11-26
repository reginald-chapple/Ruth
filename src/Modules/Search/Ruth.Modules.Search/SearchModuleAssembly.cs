using System.Reflection;

namespace Ruth.Modules.Search;

/// <summary>Assembly marker for the Search module implementation assembly.</summary>
public static class SearchModuleAssembly
{
    public static Assembly Instance => typeof(SearchModuleAssembly).Assembly;
}
