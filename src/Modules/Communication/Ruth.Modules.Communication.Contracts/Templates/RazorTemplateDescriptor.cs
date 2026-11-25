namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Compile-time descriptor for a registered Razor component template.</summary>
public sealed record RazorTemplateDescriptor(
    string Key,
    string DisplayName,
    string Description,
    string Category,
    Type ComponentType,
    IReadOnlyList<RazorTemplateParameterInfo> Parameters
);
