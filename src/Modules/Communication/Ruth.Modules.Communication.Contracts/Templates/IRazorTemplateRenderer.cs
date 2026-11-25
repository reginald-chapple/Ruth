using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Renders strongly-typed compiled .razor components to HTML strings via Blazor's HtmlRenderer.</summary>
public interface IRazorTemplateRenderer
{
    /// <summary>Renders the specified compiled Razor component type to an HTML string.</summary>
    Task<Result<string>> RenderComponentAsync<TComponent>(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Renders the specified runtime Razor component <see cref="Type" /> to an HTML string.</summary>
    Task<Result<string>> RenderComponentAsync(
        Type componentType,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Renders a registered Razor template identified by <paramref name="templateKey" />.</summary>
    Task<Result<string>> RenderNamedTemplateAsync(
        string templateKey,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default
    );
}
