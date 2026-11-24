using System.Reflection;

namespace Ruth.SharedKernel;

/// <summary>Provides a compile-time assembly anchor for SharedKernel.</summary>
public static class SharedKernelAssembly
{
    /// <summary>Gets the <see cref="SharedKernel" /> assembly instance.</summary>
    public static Assembly Instance => typeof(SharedKernelAssembly).Assembly;
}
