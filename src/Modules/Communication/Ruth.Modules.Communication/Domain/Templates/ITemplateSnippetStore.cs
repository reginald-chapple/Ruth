namespace Ruth.Modules.Communication.Domain.Templates;

/// <summary>Read port resolving active reusable snippets by normalized key during template rendering.</summary>
public interface ITemplateSnippetStore
{
    /// <summary>Finds an active snippet definition by its normalized key, or returns null when not found/inactive.</summary>
    Task<TemplateSnippetDefinition?> FindActiveByKeyAsync(
        string key,
        CancellationToken cancellationToken = default
    );
}
