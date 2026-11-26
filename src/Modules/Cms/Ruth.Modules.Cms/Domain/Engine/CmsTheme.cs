using System.Text;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>CSS custom-property bundle defining typography, palette, and layout tokens for CMS sites.</summary>
public sealed class CmsTheme : AggregateRoot<CmsThemeId>, IAuditableEntity, IHasConcurrencyToken
{
    private Dictionary<string, string> _cssCustomProperties = new(StringComparer.Ordinal);

    private CmsTheme() { }

    private CmsTheme(CmsThemeId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? CustomCss { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsSystem { get; private set; }

    public IReadOnlyDictionary<string, string> CssCustomProperties
    {
        get => _cssCustomProperties;
        private set =>
            _cssCustomProperties = new Dictionary<string, string>(value, StringComparer.Ordinal);
    }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

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

    public static Result<CmsTheme> Create(
        string name,
        string slug,
        string? description = null,
        IReadOnlyDictionary<string, string>? cssCustomProperties = null,
        string? customCss = null,
        bool isActive = true,
        bool isSystem = false
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<CmsTheme>(
                CmsErrors.Validation("Theme.NameRequired", "Theme name is required.")
            );
        }

        var normalizedSlug = CmsTextNormalizer.NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return Result.Failure<CmsTheme>(
                CmsErrors.Validation("Theme.SlugInvalid", "Theme slug is invalid.")
            );
        }

        var sanitizedTokens = new Dictionary<string, string>(StringComparer.Ordinal);
        if (cssCustomProperties is not null)
        {
            foreach (var (token, rawValue) in cssCustomProperties)
            {
                if (!CmsTextNormalizer.IsValidCssCustomPropertyName(token))
                {
                    return Result.Failure<CmsTheme>(
                        CmsErrors.Validation(
                            "Theme.TokenInvalid",
                            $"CSS custom property '{token}' must begin with '--' and use ASCII letters, digits, '-' or '_'."
                        )
                    );
                }

                sanitizedTokens[token] = CmsTextNormalizer.SanitizeCssPropertyValue(rawValue);
            }
        }

        var theme = new CmsTheme(CmsThemeId.Create())
        {
            Name = name.Trim(),
            Slug = normalizedSlug,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CustomCss = string.IsNullOrWhiteSpace(customCss) ? null : customCss.Trim(),
            IsActive = isActive,
            IsSystem = isSystem,
            _cssCustomProperties = sanitizedTokens,
        };

        return Result.Success(theme);
    }

    public string RenderCssBundle()
    {
        var sb = new StringBuilder();
        _ = sb.Append(":root {\n");
        foreach (
            var pair in _cssCustomProperties.OrderBy(static p => p.Key, StringComparer.Ordinal)
        )
        {
            _ = sb.Append("  ")
                .Append(pair.Key)
                .Append(": ")
                .Append(CmsTextNormalizer.SanitizeCssPropertyValue(pair.Value))
                .Append(";\n");
        }

        _ = sb.Append('}');
        if (!string.IsNullOrWhiteSpace(CustomCss))
        {
            _ = sb.Append("\n\n").Append(CustomCss);
        }

        return sb.ToString();
    }
}
