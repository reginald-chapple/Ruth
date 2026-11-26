using System.Reflection;

namespace Ruth.Modules.Workflow;

/// <summary>Assembly marker for Ruth.Modules.Workflow.</summary>
public static class WorkflowModuleAssembly
{
    /// <summary>Gets the Workflow implementation assembly instance.</summary>
    public static Assembly Instance => typeof(WorkflowModuleAssembly).Assembly;
}
