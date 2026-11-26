using System.Reflection;

namespace Ruth.Modules.Events.Contracts;

/// <summary>Assembly marker for the Events module public contracts.</summary>
public static class EventsContractsAssembly
{
    /// <summary>Gets the compiled <see cref="Assembly" /> for Ruth.Modules.Events.Contracts.</summary>
    public static Assembly Instance => typeof(EventsContractsAssembly).Assembly;
}
