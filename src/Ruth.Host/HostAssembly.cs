using System.Reflection;

namespace Ruth.Host;

/// <summary>Provides a compile-time assembly anchor for the Ruth host application.</summary>
public static class HostAssembly
{
    /// <summary>Gets the <see cref="Host" /> assembly instance.</summary>
    public static Assembly Instance => typeof(HostAssembly).Assembly;
}
