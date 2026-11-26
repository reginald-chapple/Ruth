using System.Text.RegularExpressions;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Domain.BulkCommunications;

/// <summary>Validated structured email composer section value model.</summary>
public sealed partial record EmailSection(
    int Order,
    EmailSectionKind Kind,
    string? Heading,
    string? BodyMarkup,
    string? ButtonLabel,
    string? ButtonUrl,
    string? SecondaryColumnMarkup,
    string? ImageUrl,
    string? ImageAltText,
    string? AccentColorHex
)
{
    private const int MaxSections = 40;

    /// <summary>Validates and normalizes a sequence of <see cref="EmailSectionDto" /> inputs.</summary>
    public static Result<IReadOnlyList<EmailSection>> ValidateAndNormalize(
        IReadOnlyList<EmailSectionDto>? inputs
    )
    {
        if (inputs is null || inputs.Count == 0)
        {
            return Result.Success<IReadOnlyList<EmailSection>>([]);
        }

        if (inputs.Count > MaxSections)
        {
            return Result.Failure<IReadOnlyList<EmailSection>>(
                BulkCommunicationErrors.EmailSectionInvalid
            );
        }

        var normalized = new List<EmailSection>(inputs.Count);
        foreach (var dto in inputs.OrderBy(static s => s.Order))
        {
            var single = ValidateSingle(dto, normalized.Count);
            if (single.IsFailure)
            {
                return Result.Failure<IReadOnlyList<EmailSection>>(single.Errors);
            }

            normalized.Add(single.Value);
        }

        return Result.Success<IReadOnlyList<EmailSection>>(normalized);
    }

    /// <summary>Projects this domain section to a contract DTO.</summary>
    public EmailSectionDto ToDto()
    {
        return new EmailSectionDto(
            Order,
            Kind,
            Heading,
            BodyMarkup,
            ButtonLabel,
            ButtonUrl,
            SecondaryColumnMarkup,
            ImageUrl,
            ImageAltText,
            AccentColorHex
        );
    }

    private static Result<EmailSection> ValidateSingle(EmailSectionDto dto, int normalizedOrder)
    {
        var accent = NormalizeHexColor(dto.AccentColorHex);
        var hasValidAccent = dto.AccentColorHex is null || accent is not null;
        return !hasValidAccent || !ValidateKindRequirements(dto)
            ? Result.Failure<EmailSection>(BulkCommunicationErrors.EmailSectionInvalid)
            : new EmailSection(
                normalizedOrder,
                dto.Kind,
                Clean(dto.Heading),
                Clean(dto.BodyMarkup),
                Clean(dto.ButtonLabel),
                Clean(dto.ButtonUrl),
                Clean(dto.SecondaryColumnMarkup),
                Clean(dto.ImageUrl),
                Clean(dto.ImageAltText),
                accent
            );
    }

    private static bool ValidateKindRequirements(EmailSectionDto dto)
    {
        return dto.Kind switch
        {
            EmailSectionKind.CallToActionButton => !string.IsNullOrWhiteSpace(dto.ButtonLabel)
                && IsSafeUrlOrMergeToken(dto.ButtonUrl),
            EmailSectionKind.ImageBlock => IsSafeUrlOrMergeToken(dto.ImageUrl)
                && !string.IsNullOrWhiteSpace(dto.ImageAltText),
            EmailSectionKind.HeroBanner or EmailSectionKind.RichText => HasAnyText(
                dto.Heading,
                dto.BodyMarkup
            ),
            EmailSectionKind.TwoColumnText => HasAnyText(dto.BodyMarkup, dto.SecondaryColumnMarkup),
            EmailSectionKind.Divider or EmailSectionKind.Footer or _ => ValidateOptionalUrls(dto),
        };
    }

    private static bool HasAnyText(string? first, string? second)
    {
        return !string.IsNullOrWhiteSpace(first) || !string.IsNullOrWhiteSpace(second);
    }

    private static bool ValidateOptionalUrls(EmailSectionDto dto)
    {
        return (dto.ButtonUrl is null || IsSafeUrlOrMergeToken(dto.ButtonUrl))
            && (dto.ImageUrl is null || IsSafeUrlOrMergeToken(dto.ImageUrl));
    }

    internal static bool IsSafeUrlOrMergeToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var trimmed = raw.Trim();
        return trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith('/')
            || (
                trimmed.StartsWith("{{", StringComparison.Ordinal)
                && trimmed.EndsWith("}}", StringComparison.Ordinal)
            );
    }

    private static string? NormalizeHexColor(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        return HexColorRegex.IsMatch(trimmed) ? trimmed.ToLowerInvariant() : null;
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [GeneratedRegex(
        @"^#[0-9a-fA-F]{6}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex HexColorRegex { get; }
}
