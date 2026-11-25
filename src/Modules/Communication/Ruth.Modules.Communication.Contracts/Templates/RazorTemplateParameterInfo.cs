namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Describes an expected parameter on a registered compiled Razor template component.</summary>
public sealed record RazorTemplateParameterInfo(
    string Name,
    string TypeName,
    bool IsRequired,
    string Description
);
