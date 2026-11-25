using System.Reflection;

namespace Ruth.Modules.Communication;

/// <summary>Assembly marker for Ruth.Modules.Communication.</summary>
public static class CommunicationModuleAssembly
{
    /// <summary>Gets the Communication module implementation assembly instance.</summary>
    public static Assembly Instance => typeof(CommunicationModuleAssembly).Assembly;
}
