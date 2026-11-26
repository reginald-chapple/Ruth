using System.Text.RegularExpressions;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Commerce.Domain.Catalog;

/// <summary>Multi-visit facility pass or credit pack definition issued through Commerce orders or staff grants.</summary>
public sealed partial class PunchPassType
    : AggregateRoot<PunchPassTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Commerce.PunchPassType";

    private PunchPassType() { }

    private PunchPassType(PunchPassTypeId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int CreditUnits { get; private set; }
    public int? ValidityDays { get; private set; }
    public Guid? AllowedBranchId { get; private set; }
    public bool AllowHouseholdSharing { get; private set; }
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

    public static Result<PunchPassType> Create(PunchPassTypeSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var entity = new PunchPassType(PunchPassTypeId.Create());
        var updated = entity.Update(spec);
        return updated.IsFailure ? Result.Failure<PunchPassType>(updated.Errors) : entity;
    }

    public Result Update(PunchPassTypeSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var normalizedCode = spec.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!IsValidSpec(spec, normalizedCode))
        {
            return CommerceErrors.PunchPassTypeInvalid;
        }

        Name = spec.Name.Trim();
        Code = normalizedCode;
        Description = string.IsNullOrWhiteSpace(spec.Description) ? null : spec.Description.Trim();
        CreditUnits = spec.CreditUnits;
        ValidityDays = spec.ValidityDays;
        AllowedBranchId = spec.AllowedBranchId is { } b && b != Guid.Empty ? b : null;
        AllowHouseholdSharing = spec.AllowHouseholdSharing;
        IsActive = spec.IsActive;
        return Result.Success();
    }

    private static bool IsValidSpec(PunchPassTypeSpec spec, string normalizedCode)
    {
        var nameAndCodeValid =
            !string.IsNullOrWhiteSpace(spec.Name)
            && spec.Name.Trim().Length <= 160
            && CodeRegex.IsMatch(normalizedCode);
        return nameAndCodeValid
            && spec.CreditUnits is >= 1 and <= 10_000
            && spec.ValidityDays is null or >= 1;
    }

    [GeneratedRegex(
        @"^[A-Z0-9][A-Z0-9_-]{1,47}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex CodeRegex { get; }
}
