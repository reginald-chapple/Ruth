using System.Globalization;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Domain.Templates;

internal static class MergeTemplateParser
{
    private const string OpenDelimiter = "{{";
    private const string CloseDelimiter = "}}";
    private const string SnippetPrefix = "snippet:";

    public static Result<ParsedMergeTemplate> Parse(
        string template,
        int maxTokenCount,
        int maxConditionalDepth
    )
    {
        var rootNodes = new List<MergeNode>();
        var frameStack = new Stack<ConditionalFrame>();
        var currentList = rootNodes;
        var tokenCount = 0;
        var index = 0;

        while (index < template.Length)
        {
            var openIdx = template.IndexOf(OpenDelimiter, index, StringComparison.Ordinal);
            if (openIdx < 0)
            {
                currentList.Add(new MergeTextNode(template[index..]));
                break;
            }

            if (openIdx > index)
            {
                currentList.Add(new MergeTextNode(template[index..openIdx]));
            }

            var step = ParseNextTag(
                template,
                openIdx,
                frameStack,
                ref currentList,
                ref tokenCount,
                maxTokenCount,
                maxConditionalDepth,
                rootNodes
            );
            if (step.IsFailure)
            {
                return Result.Failure<ParsedMergeTemplate>(step.Errors);
            }

            index = step.Value;
        }

        return frameStack.Count > 0
            ? CommunicationTemplateErrors.TemplateSyntaxError(
                "Unclosed {{#if}} conditional block; missing {{/if}}."
            )
            : new ParsedMergeTemplate(rootNodes, tokenCount);
    }

    private static Result<int> ParseNextTag(
        string template,
        int openIdx,
        Stack<ConditionalFrame> frameStack,
        ref List<MergeNode> currentList,
        ref int tokenCount,
        int maxTokenCount,
        int maxConditionalDepth,
        List<MergeNode> rootNodes
    )
    {
        var closeIdx = template.IndexOf(
            CloseDelimiter,
            openIdx + OpenDelimiter.Length,
            StringComparison.Ordinal
        );
        if (closeIdx < 0)
        {
            return CommunicationTemplateErrors.TemplateSyntaxError(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Unclosed merge token starting at position {openIdx}."
                )
            );
        }

        tokenCount++;
        if (tokenCount > maxTokenCount)
        {
            return CommunicationTemplateErrors.TokenLimitExceeded;
        }

        var rawTag = template[(openIdx + OpenDelimiter.Length)..closeIdx].Trim();
        var step = ProcessTag(rawTag, frameStack, ref currentList, maxConditionalDepth, rootNodes);
        return step.IsFailure ? Result.Failure<int>(step.Errors) : closeIdx + CloseDelimiter.Length;
    }

    private static Result ProcessTag(
        string rawTag,
        Stack<ConditionalFrame> frameStack,
        ref List<MergeNode> currentList,
        int maxConditionalDepth,
        List<MergeNode> rootNodes
    )
    {
        if (string.IsNullOrWhiteSpace(rawTag))
        {
            return CommunicationTemplateErrors.TemplateSyntaxError("Empty merge token '{{}}'.");
        }

        if (rawTag.StartsWith("#if ", StringComparison.OrdinalIgnoreCase))
        {
            return HandleIfStart(
                rawTag[4..].Trim(),
                frameStack,
                ref currentList,
                maxConditionalDepth
            );
        }

        if (string.Equals(rawTag, "#else", StringComparison.OrdinalIgnoreCase))
        {
            return HandleElse(frameStack, ref currentList);
        }

        if (string.Equals(rawTag, "/if", StringComparison.OrdinalIgnoreCase))
        {
            return HandleIfEnd(frameStack, ref currentList, rootNodes);
        }

        if (rawTag.StartsWith(SnippetPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var snippetResult = ParseSnippetTag(rawTag[SnippetPrefix.Length..].Trim());
            if (snippetResult.IsFailure)
            {
                return Result.Failure(snippetResult.Errors);
            }

            currentList.Add(snippetResult.Value);
            return Result.Success();
        }

        var fieldResult = ParseFieldTag(rawTag);
        if (fieldResult.IsFailure)
        {
            return Result.Failure(fieldResult.Errors);
        }

        currentList.Add(fieldResult.Value);
        return Result.Success();
    }

    private static Result HandleIfStart(
        string rawPath,
        Stack<ConditionalFrame> frameStack,
        ref List<MergeNode> currentList,
        int maxConditionalDepth
    )
    {
        if (frameStack.Count >= maxConditionalDepth)
        {
            return CommunicationTemplateErrors.ConditionalDepthExceeded;
        }

        var segments = ParsePathSegments(rawPath);
        if (segments.IsFailure)
        {
            return Result.Failure(segments.Errors);
        }

        var frame = new ConditionalFrame(rawPath, segments.Value, currentList);
        frameStack.Push(frame);
        currentList = frame.ThenBranch;
        return Result.Success();
    }

    private static Result HandleElse(
        Stack<ConditionalFrame> frameStack,
        ref List<MergeNode> currentList
    )
    {
        if (frameStack.Count == 0)
        {
            return CommunicationTemplateErrors.TemplateSyntaxError(
                "Unexpected {{#else}} without a matching {{#if}}."
            );
        }

        var frame = frameStack.Peek();
        if (frame.InElseBranch)
        {
            return CommunicationTemplateErrors.TemplateSyntaxError(
                "Duplicate {{#else}} inside the same {{#if}} block."
            );
        }

        frame.InElseBranch = true;
        currentList = frame.ElseBranch;
        return Result.Success();
    }

    private static Result HandleIfEnd(
        Stack<ConditionalFrame> frameStack,
        ref List<MergeNode> currentList,
        List<MergeNode> rootNodes
    )
    {
        if (frameStack.Count == 0)
        {
            return CommunicationTemplateErrors.TemplateSyntaxError(
                "Unexpected {{/if}} without a matching {{#if}}."
            );
        }

        var frame = frameStack.Pop();
        var ifNode = new MergeIfNode(
            frame.RawPath,
            frame.Segments,
            frame.ThenBranch,
            frame.ElseBranch
        );
        frame.ParentList.Add(ifNode);
        currentList = frameStack.Count > 0 ? frameStack.Peek().ActiveBranch : rootNodes;
        return Result.Success();
    }

    private static Result<MergeFieldNode> ParseFieldTag(string rawTag)
    {
        var parts = SplitUnquoted(rawTag, '|');
        if (parts.IsFailure)
        {
            return Result.Failure<MergeFieldNode>(parts.Errors);
        }

        var rawPath = parts.Value[0].Trim();
        var segments = ParsePathSegments(rawPath);
        if (segments.IsFailure)
        {
            return Result.Failure<MergeFieldNode>(segments.Errors);
        }

        var formatters = new List<MergeFormatterCall>(parts.Value.Count - 1);
        for (var i = 1; i < parts.Value.Count; i++)
        {
            var formatter = ParseFormatterCall(parts.Value[i].Trim());
            if (formatter.IsFailure)
            {
                return Result.Failure<MergeFieldNode>(formatter.Errors);
            }

            formatters.Add(formatter.Value);
        }

        return new MergeFieldNode(rawPath, segments.Value, formatters);
    }

    private static Result<MergeFormatterCall> ParseFormatterCall(string rawFormatter)
    {
        if (string.IsNullOrWhiteSpace(rawFormatter))
        {
            return CommunicationTemplateErrors.TemplateSyntaxError(
                "Empty formatter expression after '|'."
            );
        }

        var colonIdx = rawFormatter.IndexOf(':', StringComparison.Ordinal);
        if (colonIdx < 0)
        {
            var name = rawFormatter.Trim();
            return MergeFormatters.IsSupported(name)
                ? new MergeFormatterCall(name, Argument: null)
                : CommunicationTemplateErrors.UnknownFormatter(name);
        }

        var formatterName = rawFormatter[..colonIdx].Trim();
        if (!MergeFormatters.IsSupported(formatterName))
        {
            return CommunicationTemplateErrors.UnknownFormatter(formatterName);
        }

        var rawArg = rawFormatter[(colonIdx + 1)..].Trim();
        var unquoted = UnquoteIfQuoted(rawArg);
        return unquoted.IsFailure
            ? Result.Failure<MergeFormatterCall>(unquoted.Errors)
            : new MergeFormatterCall(formatterName, unquoted.Value);
    }

    private static Result<MergeSnippetNode> ParseSnippetTag(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return CommunicationTemplateErrors.TemplateSyntaxError(
                "Snippet token requires a snippet key (e.g., {{ snippet:cta_button }})."
            );
        }

        var spaceIdx = body.IndexOf(' ', StringComparison.Ordinal);
        var rawKey = spaceIdx < 0 ? body : body[..spaceIdx];
        var keyResult = TemplateSnippet.NormalizeKey(rawKey);
        if (keyResult.IsFailure)
        {
            return Result.Failure<MergeSnippetNode>(keyResult.Errors);
        }

        var arguments = new Dictionary<string, MergeArgumentValue>(
            StringComparer.OrdinalIgnoreCase
        );
        if (spaceIdx >= 0)
        {
            var parseArgs = ParseSnippetArguments(body[(spaceIdx + 1)..].Trim(), arguments);
            if (parseArgs.IsFailure)
            {
                return Result.Failure<MergeSnippetNode>(parseArgs.Errors);
            }
        }

        return new MergeSnippetNode(keyResult.Value, arguments);
    }

    private static Result ParseSnippetArguments(
        string argsText,
        Dictionary<string, MergeArgumentValue> destination
    )
    {
        var i = 0;
        while (i < argsText.Length)
        {
            while (i < argsText.Length && char.IsWhiteSpace(argsText[i]))
            {
                i++;
            }

            if (i >= argsText.Length)
            {
                break;
            }

            var eqIdx = argsText.IndexOf('=', i);
            if (eqIdx <= i)
            {
                return CommunicationTemplateErrors.TemplateSyntaxError(
                    $"Invalid snippet argument near '{argsText[i..]}'."
                );
            }

            var argName = argsText[i..eqIdx].Trim();
            if (!IsValidIdentifier(argName))
            {
                return CommunicationTemplateErrors.TemplateSyntaxError(
                    $"Invalid snippet argument name '{argName}'."
                );
            }

            i = eqIdx + 1;
            var valueResult = ReadArgumentValue(argsText, ref i);
            if (valueResult.IsFailure)
            {
                return Result.Failure(valueResult.Errors);
            }

            destination[argName] = valueResult.Value;
        }

        return Result.Success();
    }

    private static Result<MergeArgumentValue> ReadArgumentValue(string text, ref int index)
    {
        if (index >= text.Length)
        {
            return CommunicationTemplateErrors.TemplateSyntaxError(
                "Missing value for snippet argument."
            );
        }

        var ch = text[index];
        if (ch is '"' or '\'')
        {
            var start = index + 1;
            var end = text.IndexOf(ch, start);
            if (end < 0)
            {
                return CommunicationTemplateErrors.TemplateSyntaxError(
                    "Unclosed quoted snippet argument value."
                );
            }

            index = end + 1;
            return new MergeArgumentValue(IsLiteral: true, text[start..end]);
        }

        var tokenStart = index;
        while (index < text.Length && !char.IsWhiteSpace(text[index]))
        {
            index++;
        }

        return new MergeArgumentValue(IsLiteral: false, text[tokenStart..index]);
    }

    private static Result<IReadOnlyList<string>> ParsePathSegments(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return CommunicationTemplateErrors.TemplateSyntaxError("Merge field path is empty.");
        }

        var parts = rawPath.Split('.');
        for (var i = 0; i < parts.Length; i++)
        {
            if (!IsValidIdentifier(parts[i]))
            {
                return CommunicationTemplateErrors.TemplateSyntaxError(
                    $"Invalid merge field path '{rawPath}'."
                );
            }
        }

        return parts;
    }

    private static bool IsValidIdentifier(string segment)
    {
        if (string.IsNullOrEmpty(segment) || !IsIdentifierStartChar(segment[0]))
        {
            return false;
        }

        for (var i = 1; i < segment.Length; i++)
        {
            if (!IsIdentifierBodyChar(segment[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsIdentifierStartChar(char ch)
    {
        return char.IsLetter(ch) || ch == '_';
    }

    private static bool IsIdentifierBodyChar(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch is '_' or '-';
    }

    private static Result<List<string>> SplitUnquoted(string text, char separator)
    {
        var results = new List<string>();
        var start = 0;
        char? quote = null;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (TryToggleQuote(ch, ref quote))
            {
                continue;
            }

            if (!quote.HasValue && ch == separator)
            {
                results.Add(text[start..i]);
                start = i + 1;
            }
        }

        if (quote.HasValue)
        {
            return CommunicationTemplateErrors.TemplateSyntaxError(
                "Unclosed quote in merge formatter expression."
            );
        }

        results.Add(text[start..]);
        return results;
    }

    private static bool TryToggleQuote(char ch, ref char? quote)
    {
        if (quote.HasValue)
        {
            if (ch == quote.Value)
            {
                quote = null;
            }

            return true;
        }

        if (ch is '\'' or '"')
        {
            quote = ch;
            return true;
        }

        return false;
    }

    private static Result<string> UnquoteIfQuoted(string raw)
    {
        if (raw.Length == 0)
        {
            return string.Empty;
        }

        var first = raw[0];
        return first is not ('\'' or '"') ? raw
            : raw.Length < 2 || raw[^1] != first
                ? CommunicationTemplateErrors.TemplateSyntaxError(
                    "Unclosed quote in merge formatter argument."
                )
            : raw[1..^1];
    }

    private sealed class ConditionalFrame(
        string rawPath,
        IReadOnlyList<string> segments,
        List<MergeNode> parentList
    )
    {
        public string RawPath { get; } = rawPath;
        public IReadOnlyList<string> Segments { get; } = segments;
        public List<MergeNode> ParentList { get; } = parentList;
        public List<MergeNode> ThenBranch { get; } = [];
        public List<MergeNode> ElseBranch { get; } = [];
        public bool InElseBranch { get; set; }
        public List<MergeNode> ActiveBranch => InElseBranch ? ElseBranch : ThenBranch;
    }
}
