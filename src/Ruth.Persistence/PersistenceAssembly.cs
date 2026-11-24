using System.Reflection;

namespace Ruth.Persistence;

/// <summary>Assembly marker for Ruth.Persistence architecture and registration checks.</summary>
public static class PersistenceAssembly
{
    /// <summary>Gets the Ruth.Persistence assembly.</summary>
    public static Assembly Instance => typeof(PersistenceAssembly).Assembly;
}
