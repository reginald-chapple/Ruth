using System.Globalization;
using System.Text.RegularExpressions;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Communication.Domain.Templates;

/// <summary>Reusable declarative template snippet invoked via {{ snippet:key ... }} in safe merge templates.</summary>
public sealed partial class TemplateSnippet
    : AggregateRoot<TemplateSnippetId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for TemplateSnippet.</summary>
    public const string EntityTypeKeyName = "Ruth.Communication.TemplateSnippet";

    /// <summary>Maximum permitted name length.</summary>
    public const int NameMaxLength = 100;

    /// <summary>Maximum permitted snippet key length.</summary>
    public const int KeyMaxLength = 64;

    /// <summary>Maximum permitted description length.</summary>
    public const int DescriptionMaxLength = 500;

    /// <summary>Maximum permitted documentation length.</summary>
    public const int DocumentationMaxLength = 4_000;

    /// <summary>Maximum permitted markup length.</summary>
    public const int MarkupMaxLength = 65_536;

    private Dictionary<string, string> _defaultParameters = new(StringComparer.OrdinalIgnoreCase);
    private List<Guid> _categoryIds = [];

    private TemplateSnippet() { }

    private TemplateSnippet(
        TemplateSnippetId id,
        Guid systemGuid,
        string name,
        string key,
        string markup,
        string? description,
        string? documentation,
        TemplateOutputMode outputMode,
        Dictionary<string, string> defaultParameters,
        List<Guid> categoryIds,
        bool isSystem,
        bool isActive
    )
        : base(id)
    {
        SystemGuid = systemGuid;
        Name = name;
        Key = key;
        Markup = markup;
        Description = description;
        Documentation = documentation;
        OutputMode = outputMode;
        _defaultParameters = defaultParameters;
        _categoryIds = categoryIds;
        IsSystem = isSystem;
        IsActive = isActive;
    }

    public Guid SystemGuid { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Documentation { get; private set; }
    public string Markup { get; private set; } = string.Empty;
    public TemplateOutputMode OutputMode { get; private set; } = TemplateOutputMode.Html;
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyDictionary<string, string> DefaultParameters => _defaultParameters;
    public IReadOnlyList<Guid> CategoryIds => _categoryIds;

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <summary>Creates a validated reusable template snippet.</summary>
    public static Result<TemplateSnippet> Create(
        string name,
        string key,
        string markup,
        string? description = null,
        string? documentation = null,
        TemplateOutputMode outputMode = TemplateOutputMode.Html,
        IReadOnlyDictionary<string, string>? defaultParameters = null,
        IReadOnlyList<Guid>? categoryIds = null,
        bool isSystem = false,
        bool isActive = true,
        Guid? systemGuid = null
    )
    {
        var validated = ValidateInputs(name, key, markup, description, documentation);
        if (validated.IsFailure)
        {
            return Result.Failure<TemplateSnippet>(validated.Errors);
        }

        var id = TemplateSnippetId.Create();
        var (cleanName, cleanKey, cleanMarkup, cleanDescription, cleanDocs) = validated.Value;
        var snippet = new TemplateSnippet(
            id,
            systemGuid ?? id.Value,
            cleanName,
            cleanKey,
            cleanMarkup,
            cleanDescription,
            cleanDocs,
            outputMode,
            SanitizeParameters(defaultParameters),
            SanitizeCategories(categoryIds),
            isSystem,
            isActive
        );
        snippet.RaiseDomainEvent(new TemplateSnippetChangedDomainEvent(id, cleanKey, "Created"));
        return snippet;
    }

    /// <summary>Updates snippet metadata, key, markup, and default parameters.</summary>
    public Result Update(
        string name,
        string key,
        string markup,
        string? description,
        string? documentation,
        TemplateOutputMode outputMode,
        IReadOnlyDictionary<string, string>? defaultParameters,
        IReadOnlyList<Guid>? categoryIds,
        bool isActive
    )
    {
        var validated = ValidateInputs(name, key, markup, description, documentation);
        if (validated.IsFailure)
        {
            return Result.Failure(validated.Errors);
        }

        var (cleanName, cleanKey, cleanMarkup, cleanDescription, cleanDocs) = validated.Value;
        Name = cleanName;
        Key = cleanKey;
        Markup = cleanMarkup;
        Description = cleanDescription;
        Documentation = cleanDocs;
        OutputMode = outputMode;
        _defaultParameters = SanitizeParameters(defaultParameters);
        _categoryIds = SanitizeCategories(categoryIds);
        IsActive = isActive;

        RaiseDomainEvent(new TemplateSnippetChangedDomainEvent(Id, cleanKey, "Updated"));
        return Result.Success();
    }

    /// <summary>Validates that this snippet is permitted to be deleted.</summary>
    public Result EnsureCanDelete()
    {
        if (IsSystem)
        {
            return CommunicationTemplateErrors.SystemSnippetCannotBeDeleted;
        }

        RaiseDomainEvent(new TemplateSnippetChangedDomainEvent(Id, Key, "Deleted"));
        return Result.Success();
    }

    /// <summary>Normalizes and validates a snippet key slug.</summary>
    public static Result<string> NormalizeKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return CommunicationTemplateErrors.SnippetKeyInvalid;
        }

        var normalized = key.Trim().ToLower(CultureInfo.InvariantCulture);
        return !SnippetKeyRegex.IsMatch(normalized)
            ? CommunicationTemplateErrors.SnippetKeyInvalid
            : normalized;
    }

    /// <summary>Creates a lightweight immutable runtime definition for merge rendering.</summary>
    public TemplateSnippetDefinition ToDefinition()
    {
        return new TemplateSnippetDefinition(Key, Markup, OutputMode, _defaultParameters);
    }

    private static Result<(
        string Name,
        string Key,
        string Markup,
        string? Description,
        string? Documentation
    )> ValidateInputs(
        string name,
        string key,
        string markup,
        string? description,
        string? documentation
    )
    {
        var nameResult = ValidateRequiredField(
            name,
            NameMaxLength,
            CommunicationTemplateErrors.SnippetNameRequired,
            CommunicationTemplateErrors.SnippetNameTooLong
        );
        if (nameResult.IsFailure)
        {
            return Result.Failure<(string, string, string, string?, string?)>(nameResult.Errors);
        }

        var keyResult = NormalizeKey(key);
        if (keyResult.IsFailure)
        {
            return Result.Failure<(string, string, string, string?, string?)>(keyResult.Errors);
        }

        var markupResult = ValidateMarkup(markup);
        if (markupResult.IsFailure)
        {
            return Result.Failure<(string, string, string, string?, string?)>(markupResult.Errors);
        }

        var descResult = ValidateOptionalText(
            description,
            DescriptionMaxLength,
            CommunicationTemplateErrors.SnippetDescriptionTooLong
        );
        if (descResult.IsFailure)
        {
            return Result.Failure<(string, string, string, string?, string?)>(descResult.Errors);
        }

        var docsResult = ValidateOptionalText(
            documentation,
            DocumentationMaxLength,
            CommunicationTemplateErrors.SnippetDocumentationTooLong
        );
        return docsResult.IsFailure
            ? Result.Failure<(string, string, string, string?, string?)>(docsResult.Errors)
            : (
                nameResult.Value,
                keyResult.Value,
                markupResult.Value,
                descResult.Value,
                docsResult.Value
            );
    }

    private static Result<string> ValidateRequiredField(
        string value,
        int maxLength,
        DomainError requiredError,
        DomainError tooLongError
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return requiredError;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? tooLongError : trimmed;
    }

    private static Result<string> ValidateMarkup(string markup)
    {
        return string.IsNullOrWhiteSpace(markup) ? CommunicationTemplateErrors.SnippetMarkupRequired
            : markup.Length > MarkupMaxLength ? CommunicationTemplateErrors.SnippetMarkupTooLong
            : markup;
    }

    private static Result<string?> ValidateOptionalText(
        string? value,
        int maxLength,
        DomainError tooLongError
    )
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return trimmed is not null && trimmed.Length > maxLength
            ? tooLongError
            : Result.Success(trimmed);
    }

    private static Dictionary<string, string> SanitizeParameters(
        IReadOnlyDictionary<string, string>? input
    )
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (input is null)
        {
            return result;
        }

        foreach (var pair in input)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                result[pair.Key.Trim()] = pair.Value ?? string.Empty;
            }
        }

        return result;
    }

    private static List<Guid> SanitizeCategories(IReadOnlyList<Guid>? categoryIds)
    {
        return categoryIds is null || categoryIds.Count == 0
            ? []
            : [.. categoryIds.Where(static id => id != Guid.Empty).Distinct()];
    }

    [GeneratedRegex(
        "^[a-z][a-z0-9_-]{1,63}$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 100
    )]
    private static partial Regex SnippetKeyRegex { get; }
}
