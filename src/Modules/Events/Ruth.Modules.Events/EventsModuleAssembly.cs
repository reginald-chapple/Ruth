using System.Reflection;

namespace Ruth.Modules.Events;

/// <summary>Assembly marker for the Events module implementation.</summary>
public static class EventsModuleAssembly
{
    /// <summary>Gets the compiled <see cref="Assembly" /> for Ruth.Modules.Events.</summary>
    public static Assembly Instance => typeof(EventsModuleAssembly).Assembly;
}
