using System.Globalization;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.Groups;

/// <summary>Stores a typed demographic value associated with a Group and GroupDemographicType.</summary>
public sealed class GroupDemographicValue : AggregateRoot<GroupDemographicValueId>, IAuditableEntity
{
    private GroupDemographicValue() { }

    private GroupDemographicValue(
        GroupDemographicValueId id,
        GroupId groupId,
        GroupDemographicTypeId groupDemographicTypeId
    )
        : base(id)
    {
        GroupId = groupId;
        GroupDemographicTypeId = groupDemographicTypeId;
    }

    /// <summary>Gets the Group identifier.</summary>
    public GroupId GroupId { get; private set; }

    /// <summary>Gets the GroupDemographicType identifier.</summary>
    public GroupDemographicTypeId GroupDemographicTypeId { get; private set; }

    /// <summary>Gets the raw string representation of the demographic value.</summary>
    public string? Value { get; private set; }

    /// <summary>Gets the parsed numeric representation when applicable.</summary>
    public decimal? ValueAsNumeric { get; private set; }

    /// <summary>Gets the parsed boolean representation when applicable.</summary>
    public bool? ValueAsBoolean { get; private set; }

    /// <summary>Gets the parsed GUID representation when applicable.</summary>
    public Guid? ValueAsGuid { get; private set; }

    /// <summary>Gets the optional related entity type key.</summary>
    public string? RelatedEntityTypeKey { get; private set; }

    /// <summary>Gets the optional related entity identifier.</summary>
    public Guid? RelatedEntityId { get; private set; }

    /// <summary>Gets when this demographic value was last calculated or updated.</summary>
    public DateTimeOffset? LastCalculatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

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

    internal static Result<GroupDemographicValue> Create(
        GroupId groupId,
        GroupDemographicTypeId groupDemographicTypeId,
        string? value,
        string? relatedEntityTypeKey,
        Guid? relatedEntityId,
        DateTimeOffset nowUtc
    )
    {
        var item = new GroupDemographicValue(
            GroupDemographicValueId.Create(),
            groupId,
            groupDemographicTypeId
        );
        var update = item.Update(value, relatedEntityTypeKey, relatedEntityId, nowUtc);
        return update.IsFailure
            ? Result.Failure<GroupDemographicValue>(update.Errors)
            : Result.Success(item);
    }

    internal Result Update(
        string? value,
        string? relatedEntityTypeKey,
        Guid? relatedEntityId,
        DateTimeOffset nowUtc
    )
    {
        var valResult = GroupsTextPolicy.NormalizeOptional(
            value,
            GroupsTextPolicy.DescriptionMaxLength,
            allowNewLines: false
        );
        var keyResult = GroupsTextPolicy.NormalizeOptional(
            relatedEntityTypeKey,
            GroupsTextPolicy.NameMaxLength,
            allowNewLines: false
        );
        var combined = Result.Combine(valResult, keyResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        Value = valResult.Value;
        RelatedEntityTypeKey = keyResult.Value;
        RelatedEntityId = relatedEntityId;
        LastCalculatedOnUtc = nowUtc.ToUniversalTime();
        ParseTypedProjections(valResult.Value);
        return Result.Success();
    }

    private void ParseTypedProjections(string? raw)
    {
        ValueAsNumeric =
            raw is not null
            && raw.Length <= 24
            && decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var num)
                ? num
                : null;
        ValueAsBoolean = raw is not null && bool.TryParse(raw, out var flag) ? flag : null;
        ValueAsGuid =
            raw is not null && raw.Length is >= 32 and <= 36 && Guid.TryParse(raw, out var guid)
                ? guid
                : null;
    }
}
