using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Domain.Templates;

/// <summary>
/// Pure-C# safe declarative merge-token evaluator with bounded AST caching, XSS encoding,
/// strict output/token/depth limits, and cycle-checked reusable snippet expansion.
/// </summary>
internal sealed class SafeMergeTemplateEngine(ITemplateSnippetStore snippetStore)
    : IMergeTemplateEngine
{
    private const int MaxCacheEntries = 2_048;

    private static readonly ConcurrentDictionary<string, ParsedMergeTemplate> _parseCache = new(
        StringComparer.Ordinal
    );

    public async Task<Result<MergeRenderResult>> RenderAsync(
        string templateMarkup,
        IReadOnlyDictionary<string, object?>? context = null,
        MergeRenderOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(templateMarkup);

        var effectiveOptions = options ?? MergeRenderOptions.DefaultHtml;
        if (templateMarkup.Length > effectiveOptions.MaxTemplateLength)
        {
            return CommunicationTemplateErrors.TemplateTooLarge;
        }

        var parsedResult = GetOrParse(templateMarkup, effectiveOptions);
        if (parsedResult.IsFailure)
        {
            return Result.Failure<MergeRenderResult>(parsedResult.Errors);
        }

        var culture = ResolveCulture(effectiveOptions.CultureName);
        var state = new RenderState(
            effectiveOptions,
            culture,
            new StringBuilder(Math.Min(templateMarkup.Length * 2, 4_096))
        );

        var evalResult = await EvaluateNodesAsync(
            parsedResult.Value.Nodes,
            context,
            effectiveOptions.OutputMode,
            state,
            cancellationToken
        );
        return evalResult.IsFailure ? Result.Failure<MergeRenderResult>(evalResult.Errors)
            : effectiveOptions.StrictMissingTokens && state.MissingTokens.Count > 0
                ? CommunicationTemplateErrors.MissingRequiredTokens(state.MissingTokens)
            : new MergeRenderResult(
                state.Output.ToString(),
                state.MissingTokens,
                state.SnippetsExpanded
            );
    }

    private static Result<ParsedMergeTemplate> GetOrParse(
        string template,
        MergeRenderOptions options
    )
    {
        var cacheKey = string.Create(
            CultureInfo.InvariantCulture,
            $"{options.MaxTokenCount}:{options.MaxConditionalDepth}:{template}"
        );
        if (_parseCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var parsed = MergeTemplateParser.Parse(
            template,
            options.MaxTokenCount,
            options.MaxConditionalDepth
        );
        if (parsed.IsSuccess)
        {
            if (_parseCache.Count >= MaxCacheEntries)
            {
                _parseCache.Clear();
            }

            _parseCache[cacheKey] = parsed.Value;
        }

        return parsed;
    }

    private async Task<Result> EvaluateNodesAsync(
        IReadOnlyList<MergeNode> nodes,
        IReadOnlyDictionary<string, object?>? context,
        TemplateOutputMode outputMode,
        RenderState state,
        CancellationToken cancellationToken
    )
    {
        foreach (var node in nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stepResult = node switch
            {
                MergeTextNode text => AppendBounded(state, text.Text),
                MergeFieldNode field => EvaluateField(field, context, outputMode, state),
                MergeIfNode conditional => await EvaluateIfAsync(
                    conditional,
                    context,
                    outputMode,
                    state,
                    cancellationToken
                ),
                MergeSnippetNode snippet => await EvaluateSnippetAsync(
                    snippet,
                    context,
                    state,
                    cancellationToken
                ),
                _ => Result.Success(),
            };

            if (stepResult.IsFailure)
            {
                return stepResult;
            }
        }

        return Result.Success();
    }

    private static Result EvaluateField(
        MergeFieldNode field,
        IReadOnlyDictionary<string, object?>? context,
        TemplateOutputMode outputMode,
        RenderState state
    )
    {
        var found = MergeValueResolver.TryResolvePath(
            context,
            field.PathSegments,
            out var rawValue
        );
        if (!found)
        {
            state.RecordMissingToken(field.RawPath);
        }

        var formatted = MergeFormatters.ApplyChain(
            rawValue,
            field.Formatters,
            state.Culture,
            outputMode
        );
        var scrubbed = ScrubUnsafeControlCharacters(formatted.Text);
        var finalText =
            outputMode == TemplateOutputMode.Html && !formatted.AlreadyHtmlEncoded
                ? HtmlTextEncoder.Encode(scrubbed)
                : scrubbed;

        return AppendBounded(state, finalText);
    }

    private static string ScrubUnsafeControlCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (ch is '\r' or '\n' or '\t' || !char.IsControl(ch))
            {
                _ = sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    private Task<Result> EvaluateIfAsync(
        MergeIfNode conditional,
        IReadOnlyDictionary<string, object?>? context,
        TemplateOutputMode outputMode,
        RenderState state,
        CancellationToken cancellationToken
    )
    {
        _ = MergeValueResolver.TryResolvePath(
            context,
            conditional.ConditionSegments,
            out var conditionValue
        );
        var branch = MergeValueResolver.IsTruthy(conditionValue)
            ? conditional.ThenBranch
            : conditional.ElseBranch;
        return EvaluateNodesAsync(branch, context, outputMode, state, cancellationToken);
    }

    private async Task<Result> EvaluateSnippetAsync(
        MergeSnippetNode snippetNode,
        IReadOnlyDictionary<string, object?>? parentContext,
        RenderState state,
        CancellationToken cancellationToken
    )
    {
        if (state.ActiveSnippets.Contains(snippetNode.SnippetKey))
        {
            return CommunicationTemplateErrors.SnippetCycleDetected;
        }

        if (state.ActiveSnippets.Count >= state.Options.MaxSnippetDepth)
        {
            return CommunicationTemplateErrors.SnippetDepthExceeded;
        }

        var definition = await snippetStore.FindActiveByKeyAsync(
            snippetNode.SnippetKey,
            cancellationToken
        );
        if (definition is null)
        {
            return CommunicationTemplateErrors.UnknownSnippet(snippetNode.SnippetKey);
        }

        var parsed = GetOrParse(definition.Markup, state.Options);
        if (parsed.IsFailure)
        {
            return Result.Failure(parsed.Errors);
        }

        var snippetContext = BuildSnippetContext(parentContext, definition, snippetNode.Arguments);
        _ = state.ActiveSnippets.Add(snippetNode.SnippetKey);
        state.SnippetsExpanded++;
        try
        {
            return await EvaluateNodesAsync(
                parsed.Value.Nodes,
                snippetContext,
                definition.OutputMode,
                state,
                cancellationToken
            );
        }
        finally
        {
            _ = state.ActiveSnippets.Remove(snippetNode.SnippetKey);
        }
    }

    private static Dictionary<string, object?> BuildSnippetContext(
        IReadOnlyDictionary<string, object?>? parentContext,
        TemplateSnippetDefinition definition,
        IReadOnlyDictionary<string, MergeArgumentValue> callArguments
    )
    {
        var scoped = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (parentContext is not null)
        {
            foreach (var pair in parentContext)
            {
                scoped[pair.Key] = pair.Value;
            }
        }

        foreach (var defaultParam in definition.DefaultParameters)
        {
            scoped[defaultParam.Key] = defaultParam.Value;
        }

        foreach (var callArg in callArguments)
        {
            if (callArg.Value.IsLiteral)
            {
                scoped[callArg.Key] = callArg.Value.Value;
            }
            else if (
                MergeValueResolver.TryResolvePath(
                    parentContext,
                    callArg.Value.Value.Split('.'),
                    out var resolved
                )
            )
            {
                scoped[callArg.Key] = resolved;
            }
        }

        return scoped;
    }

    private static Result AppendBounded(RenderState state, string text)
    {
        if (state.Output.Length + text.Length > state.Options.MaxOutputLength)
        {
            return CommunicationTemplateErrors.OutputLimitExceeded;
        }

        _ = state.Output.Append(text);
        return Result.Success();
    }

    private static CultureInfo ResolveCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return CultureInfo.GetCultureInfo("en-US");
        }

        try
        {
            return CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }

    private sealed class RenderState(
        MergeRenderOptions options,
        CultureInfo culture,
        StringBuilder output
    )
    {
        private readonly HashSet<string> _missingSet = new(StringComparer.OrdinalIgnoreCase);

        public MergeRenderOptions Options { get; } = options;
        public CultureInfo Culture { get; } = culture;
        public StringBuilder Output { get; } = output;
        public List<string> MissingTokens { get; } = [];
        public HashSet<string> ActiveSnippets { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int SnippetsExpanded { get; set; }

        public void RecordMissingToken(string rawPath)
        {
            if (_missingSet.Add(rawPath))
            {
                MissingTokens.Add(rawPath);
            }
        }
    }
}
