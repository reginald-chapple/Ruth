using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Domain.Templates;

/// <summary>Domain errors for Communication templating, merge rendering, and reusable snippets.</summary>
public static class CommunicationTemplateErrors
{
    public static readonly DomainError SnippetNameRequired = DomainError.Validation(
        "Communication.SnippetNameRequired",
        "Snippet name is required."
    );

    public static readonly DomainError SnippetNameTooLong = DomainError.Validation(
        "Communication.SnippetNameTooLong",
        "Snippet name cannot exceed 100 characters."
    );

    public static readonly DomainError SnippetKeyInvalid = DomainError.Validation(
        "Communication.SnippetKeyInvalid",
        "Snippet key must be 2–64 lowercase letters, digits, hyphens, or underscores starting with a letter."
    );

    public static readonly DomainError SnippetKeyDuplicate = DomainError.Conflict(
        "Communication.SnippetKeyDuplicate",
        "A template snippet with the specified key already exists."
    );

    public static readonly DomainError SnippetMarkupRequired = DomainError.Validation(
        "Communication.SnippetMarkupRequired",
        "Snippet markup is required."
    );

    public static readonly DomainError SnippetMarkupTooLong = DomainError.Validation(
        "Communication.SnippetMarkupTooLong",
        "Snippet markup cannot exceed 65,536 characters."
    );

    public static readonly DomainError SnippetDescriptionTooLong = DomainError.Validation(
        "Communication.SnippetDescriptionTooLong",
        "Snippet description cannot exceed 500 characters."
    );

    public static readonly DomainError SnippetDocumentationTooLong = DomainError.Validation(
        "Communication.SnippetDocumentationTooLong",
        "Snippet documentation cannot exceed 4,000 characters."
    );

    public static readonly DomainError SnippetNotFound = DomainError.NotFound(
        "Communication.SnippetNotFound",
        "Template snippet was not found."
    );

    public static readonly DomainError SystemSnippetCannotBeDeleted = DomainError.Conflict(
        "Communication.SystemSnippetCannotBeDeleted",
        "System template snippets cannot be deleted."
    );

    public static readonly DomainError TemplateTooLarge = DomainError.Validation(
        "Communication.TemplateTooLarge",
        "Template exceeds the maximum permitted length."
    );

    public static readonly DomainError OutputLimitExceeded = DomainError.Validation(
        "Communication.OutputLimitExceeded",
        "Rendered template output exceeded the maximum permitted length."
    );

    public static readonly DomainError TokenLimitExceeded = DomainError.Validation(
        "Communication.TokenLimitExceeded",
        "Template exceeds the maximum permitted number of merge tokens."
    );

    public static readonly DomainError ConditionalDepthExceeded = DomainError.Validation(
        "Communication.ConditionalDepthExceeded",
        "Template conditional nesting exceeds the maximum permitted depth."
    );

    public static readonly DomainError SnippetDepthExceeded = DomainError.Validation(
        "Communication.SnippetDepthExceeded",
        "Snippet expansion exceeded the maximum permitted recursion depth."
    );

    public static readonly DomainError SnippetCycleDetected = DomainError.Validation(
        "Communication.SnippetCycleDetected",
        "Circular snippet reference detected during template rendering."
    );

    public static DomainError TemplateSyntaxError(string detail)
    {
        return DomainError.Validation("Communication.TemplateSyntaxError", detail);
    }

    public static DomainError UnknownFormatter(string formatterName)
    {
        return DomainError.Validation(
            "Communication.UnknownFormatter",
            $"Merge formatter '{formatterName}' is not supported."
        );
    }

    public static DomainError UnknownSnippet(string snippetKey)
    {
        return DomainError.NotFound(
            "Communication.UnknownSnippet",
            $"Active template snippet '{snippetKey}' was not found."
        );
    }

    public static DomainError MissingRequiredTokens(IReadOnlyList<string> missingTokens)
    {
        return DomainError.Validation(
            "Communication.MissingTokens",
            $"Template contains unresolved merge tokens: {string.Join(", ", missingTokens)}."
        );
    }

    public static DomainError InvalidContextJson(string detail)
    {
        return DomainError.Validation(
            "Communication.InvalidContextJson",
            $"Invalid JSON context payload: {detail}"
        );
    }

    public static DomainError InvalidRazorComponentType(Type componentType)
    {
        return DomainError.Validation(
            "Communication.InvalidRazorComponentType",
            $"Type '{componentType.FullName}' does not implement Microsoft.AspNetCore.Components.IComponent."
        );
    }

    public static DomainError RazorTemplateNotFound(string templateKey)
    {
        return DomainError.NotFound(
            "Communication.RazorTemplateNotFound",
            $"Registered Razor template '{templateKey}' was not found."
        );
    }

    public static DomainError RazorRenderFailed(string templateName, string detail)
    {
        return DomainError.Failure(
            "Communication.RazorRenderFailed",
            $"Failed to render Razor template '{templateName}': {detail}"
        );
    }
}
