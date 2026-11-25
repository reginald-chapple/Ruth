namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Registry of compile-time discovered Razor component templates available for HTML rendering.</summary>
public interface IRazorTemplateRegistry
{
    /// <summary>Returns all registered Razor template descriptors ordered by category and display name.</summary>
    IReadOnlyList<RazorTemplateDescriptor> GetAll();

    /// <summary>Finds a registered Razor template descriptor by its unique case-insensitive key.</summary>
    RazorTemplateDescriptor? FindByKey(string templateKey);
}
