using System.Reflection;

namespace Ruth.Modules.VirtualRooms;

/// <summary>Assembly marker for Ruth.Modules.VirtualRooms.</summary>
public static class VirtualRoomsModuleAssembly
{
    /// <summary>Gets the VirtualRooms implementation assembly instance.</summary>
    public static Assembly Instance => typeof(VirtualRoomsModuleAssembly).Assembly;
}
