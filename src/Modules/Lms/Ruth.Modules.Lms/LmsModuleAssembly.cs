using System.Reflection;

namespace Ruth.Modules.Lms;

/// <summary>Assembly marker for the LMS module implementation assembly.</summary>
public static class LmsModuleAssembly
{
    public static Assembly Instance => typeof(LmsModuleAssembly).Assembly;
}
