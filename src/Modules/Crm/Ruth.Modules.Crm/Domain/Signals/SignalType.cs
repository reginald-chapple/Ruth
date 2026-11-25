using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Crm.Domain.Signals;

/// <summary>Configures a prioritized safety/VIP banner signal type (lower <see cref="Order" /> = higher priority).</summary>
public sealed class SignalType
    : AggregateRoot<SignalTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for SignalType.</summary>
    public const string EntityTypeKeyName = "Ruth.Crm.SignalType";

    /// <summary>Fallback icon name matching Rock RMS default flag icon.</summary>
    public const string DefaultSignalIcon = "flag";

    private SignalType() { }

    private SignalType(
        SignalTypeId id,
        string name,
        string? description,
        string signalColor,
        string signalIcon,
        int order
    )
        : base(id)
    {
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Description = description;
        SignalColor = signalColor;
        SignalIcon = signalIcon;
        Order = Math.Max(0, order);
    }

    /// <summary>Gets the unique signal type name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the uppercase normalized name.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the banner hex/CSS color.</summary>
    public string SignalColor { get; private set; } = "#D13438";

    /// <summary>Gets the banner icon name or CSS class.</summary>
    public string SignalIcon { get; private set; } = DefaultSignalIcon;

    /// <summary>Gets the display priority (lower wins as TopSignal).</summary>
    public int Order { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Creates a validated signal type.</summary>
    public static Result<SignalType> Create(
        string name,
        string signalColor,
        string? signalIcon = null,
        string? description = null,
        int order = 0
    )
    {
        var nameResult = CrmTextPolicy.ValidateRequiredText(
            name,
            CrmTextPolicy.SignalNameMaxLength,
            CrmErrors.SignalTypeNameRequired,
            CrmErrors.TextTooLong
        );
        var colorResult = CrmTextPolicy.ValidateRequiredText(
            signalColor,
            100,
            CrmErrors.SignalTypeColorRequired,
            CrmErrors.TextTooLong
        );
        var descResult = CrmTextPolicy.ValidateOptionalText(
            description,
            CrmTextPolicy.DescriptionMaxLength
        );
        var combined = Result.Combine(nameResult, colorResult, descResult);
        if (combined.IsFailure)
        {
            return Result.Failure<SignalType>(combined.Errors);
        }

        var icon = string.IsNullOrWhiteSpace(signalIcon) ? DefaultSignalIcon : signalIcon.Trim();
        return Result.Success(
            new SignalType(
                SignalTypeId.Create(),
                nameResult.Value,
                descResult.Value,
                colorResult.Value,
                icon,
                order
            )
        );
    }

    /// <summary>Updates signal type display properties and priority.</summary>
    public Result Update(
        string name,
        string signalColor,
        string? signalIcon,
        string? description,
        int order
    )
    {
        var nameResult = CrmTextPolicy.ValidateRequiredText(
            name,
            CrmTextPolicy.SignalNameMaxLength,
            CrmErrors.SignalTypeNameRequired,
            CrmErrors.TextTooLong
        );
        var colorResult = CrmTextPolicy.ValidateRequiredText(
            signalColor,
            100,
            CrmErrors.SignalTypeColorRequired,
            CrmErrors.TextTooLong
        );
        var descResult = CrmTextPolicy.ValidateOptionalText(
            description,
            CrmTextPolicy.DescriptionMaxLength
        );
        var combined = Result.Combine(nameResult, colorResult, descResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        Name = nameResult.Value;
        NormalizedName = Name.ToUpperInvariant();
        SignalColor = colorResult.Value;
        SignalIcon = string.IsNullOrWhiteSpace(signalIcon) ? DefaultSignalIcon : signalIcon.Trim();
        Description = descResult.Value;
        Order = Math.Max(0, order);
        return Result.Success();
    }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }
}
