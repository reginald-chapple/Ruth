namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Boolean combination mode for evaluating rules inside a RequestFilter.</summary>
public enum RequestFilterMode
{
    /// <summary>Every rule in the filter must match.</summary>
    AllMatch = 0,

    /// <summary>At least one rule in the filter must match.</summary>
    AnyMatch = 1,
}
