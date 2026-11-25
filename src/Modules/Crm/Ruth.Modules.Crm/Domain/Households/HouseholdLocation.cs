using Ruth.Modules.Crm.Contracts.Households;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Households;

/// <summary>Links a <see cref="Household" /> to a physical Core Location (Home, Mailing, Work, Previous).</summary>
public sealed class HouseholdLocation : Entity<HouseholdLocationId>, IAuditableEntity
{
    private HouseholdLocation() { }

    internal HouseholdLocation(
        HouseholdLocationId id,
        HouseholdId householdId,
        Guid locationId,
        HouseholdLocationType locationType,
        bool isMailingLocation,
        bool isMappedLocation,
        string? addressSummary
    )
        : base(id)
    {
        HouseholdId = householdId;
        LocationId = locationId;
        LocationType = locationType;
        IsMailingLocation = isMailingLocation;
        IsMappedLocation = isMappedLocation;
        AddressSummary = addressSummary;
    }

    /// <summary>Gets the owning household identifier.</summary>
    public HouseholdId HouseholdId { get; private set; }

    /// <summary>Gets the referenced Core Location identifier.</summary>
    public Guid LocationId { get; private set; }

    /// <summary>Gets the address classification.</summary>
    public HouseholdLocationType LocationType { get; private set; }

    /// <summary>Gets whether statements and mailings are sent to this location.</summary>
    public bool IsMailingLocation { get; private set; }

    /// <summary>Gets whether this location is the primary mapped geocoded address.</summary>
    public bool IsMappedLocation { get; private set; }

    /// <summary>Gets an optional human-readable address summary snapshot.</summary>
    public string? AddressSummary { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    internal void DemoteToPrevious()
    {
        LocationType = HouseholdLocationType.Previous;
        IsMailingLocation = false;
        IsMappedLocation = false;
    }

    internal void Update(
        HouseholdLocationType locationType,
        bool isMailingLocation,
        bool isMappedLocation,
        string? addressSummary
    )
    {
        LocationType = locationType;
        IsMailingLocation = isMailingLocation;
        IsMappedLocation = isMappedLocation;
        AddressSummary = addressSummary;
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
}
