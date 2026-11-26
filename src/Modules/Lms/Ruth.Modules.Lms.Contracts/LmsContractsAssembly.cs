using System.Reflection;

namespace Ruth.Modules.Lms.Contracts;

/// <summary>Assembly marker for the LMS module contracts assembly.</summary>
public static class LmsContractsAssembly
{
    /// <summary>Gets the LMS contracts assembly reference.</summary>
    public static Assembly Instance => typeof(LmsContractsAssembly).Assembly;
}
