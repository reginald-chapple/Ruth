namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>
/// Pure contract implemented by modular Blazor CMS block components rendered inside
/// CmsPageHost zones via DynamicComponent.
/// </summary>
public interface IRuthBlock
{
    /// <summary>Gets the canonical unique block type key (e.g., cms.html-content).</summary>
    string BlockKey { get; }

    /// <summary>Gets the human-readable display name shown in the CMS Zone/Block configurator.</summary>
    string DisplayName { get; }

    /// <summary>Gets the UI category grouping for the block configurator palette.</summary>
    string Category { get; }

    /// <summary>Gets an optional description of the block's purpose.</summary>
    string? Description { get; }

    /// <summary>Gets whether this block requires interactive server islands instead of Static SSR.</summary>
    bool RequiresInteractivity { get; }
}
