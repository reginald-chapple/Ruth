using System.Text.RegularExpressions;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.CheckIn.Domain.WalletPasses;

/// <summary>Smart wallet pass template aggregate defining branding, PassKit type identifier, and default card layout.</summary>
public sealed partial class WalletPassTemplate
    : AggregateRoot<WalletPassTemplateId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type registration key.</summary>
    public const string EntityTypeKeyName = "Ruth.CheckIn.WalletPassTemplate";

    private WalletPassTemplate() { }

    private WalletPassTemplate(WalletPassTemplateId id, Guid systemGuid, string key, bool isSystem)
        : base(id)
    {
        SystemGuid = systemGuid;
        Key = key;
        IsSystem = isSystem;
    }

    public Guid SystemGuid { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string PassTypeIdentifier { get; private set; } = string.Empty;
    public string OrganizationName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public WalletPassCardStyle CardStyle { get; private set; }
    public string BackgroundColorHex { get; private set; } = "#1E3A8A";
    public string ForegroundColorHex { get; private set; } = "#FFFFFF";
    public string LabelColorHex { get; private set; } = "#BFDBFE";
    public string? LogoText { get; private set; }
    public int? DefaultValidityDays { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; } = true;

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

    /// <summary>Creates a validated <see cref="WalletPassTemplate" /> aggregate.</summary>
    public static Result<WalletPassTemplate> Create(WalletPassTemplateSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (string.IsNullOrWhiteSpace(spec.Key) || !SlugRegex.IsMatch(spec.Key.Trim()))
        {
            return Result.Failure<WalletPassTemplate>(WalletPassErrors.TemplateKeyInvalid);
        }

        var id = WalletPassTemplateId.Create();
        var template = new WalletPassTemplate(
            id,
            spec.SystemGuid ?? id.Value,
            spec.Key.Trim(),
            spec.IsSystem
        );
        var updated = template.Update(spec);
        return updated.IsFailure ? Result.Failure<WalletPassTemplate>(updated.Errors) : template;
    }

    /// <summary>Validates and updates mutable fields on this template.</summary>
    public Result Update(WalletPassTemplateSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var validation = ValidateMutableSpec(spec);
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = spec.Name.Trim();
        PassTypeIdentifier = spec.PassTypeIdentifier.Trim().ToLowerInvariant();
        OrganizationName = spec.OrganizationName.Trim();
        Description = spec.Description.Trim();
        CardStyle = spec.CardStyle;
        BackgroundColorHex = spec.BackgroundColorHex.Trim().ToUpperInvariant();
        ForegroundColorHex = spec.ForegroundColorHex.Trim().ToUpperInvariant();
        LabelColorHex = spec.LabelColorHex.Trim().ToUpperInvariant();
        LogoText = string.IsNullOrWhiteSpace(spec.LogoText) ? null : spec.LogoText.Trim();
        DefaultValidityDays = spec.DefaultValidityDays;
        IsActive = spec.IsActive;
        return Result.Success();
    }

    /// <summary>Ensures non-system status before deletion.</summary>
    public Result EnsureCanDelete()
    {
        return IsSystem ? WalletPassErrors.SystemTemplateProtected : Result.Success();
    }

    private static Result ValidateMutableSpec(WalletPassTemplateSpec spec)
    {
        var textCheck = ValidateTextMetadata(spec);
        return textCheck.IsFailure ? textCheck : ValidateStyleMetadata(spec);
    }

    private static Result ValidateTextMetadata(WalletPassTemplateSpec spec)
    {
        return !HasValidName(spec.Name) ? WalletPassErrors.TemplateNameInvalid
            : !HasValidPassType(spec.PassTypeIdentifier)
                ? WalletPassErrors.PassTypeIdentifierInvalid
            : !HasValidOrgAndDescription(spec.OrganizationName, spec.Description)
                ? WalletPassErrors.OrganizationOrDescriptionInvalid
            : Result.Success();
    }

    private static Result ValidateStyleMetadata(WalletPassTemplateSpec spec)
    {
        return !HasValidColors(spec) ? WalletPassErrors.ColorHexInvalid
            : spec.DefaultValidityDays is < 1 or > 3650 ? WalletPassErrors.ValidityDaysInvalid
            : Result.Success();
    }

    private static bool HasValidName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 120;
    }

    private static bool HasValidPassType(string? passType)
    {
        return !string.IsNullOrWhiteSpace(passType) && PassTypeRegex.IsMatch(passType.Trim());
    }

    private static bool HasValidOrgAndDescription(string? org, string? description)
    {
        return !string.IsNullOrWhiteSpace(org)
            && org.Trim().Length <= 120
            && !string.IsNullOrWhiteSpace(description)
            && description.Trim().Length <= 300;
    }

    private static bool HasValidColors(WalletPassTemplateSpec spec)
    {
        return IsValidHexColor(spec.BackgroundColorHex)
            && IsValidHexColor(spec.ForegroundColorHex)
            && IsValidHexColor(spec.LabelColorHex);
    }

    private static bool IsValidHexColor(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && HexColorRegex.IsMatch(value.Trim());
    }

    [GeneratedRegex(
        @"^[a-z][a-z0-9._-]{2,79}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex SlugRegex { get; }

    [GeneratedRegex(
        @"^pass\.[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)+$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex PassTypeRegex { get; }

    [GeneratedRegex(
        @"^#[0-9a-fA-F]{6}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex HexColorRegex { get; }
}
