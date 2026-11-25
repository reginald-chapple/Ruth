using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>
/// Evaluates database-stored declarative merge templates ({{ Path | formatter }}, {{#if}}, and {{ snippet:key }})
/// over safe read-only DTO contexts without arbitrary runtime scripting.
/// </summary>
public interface IMergeTemplateEngine
{
    /// <summary>Renders <paramref name="templateMarkup" /> against the supplied read-only context dictionary.</summary>
    Task<Result<MergeRenderResult>> RenderAsync(
        string templateMarkup,
        IReadOnlyDictionary<string, object?>? context = null,
        MergeRenderOptions? options = null,
        CancellationToken cancellationToken = default
    );
}
