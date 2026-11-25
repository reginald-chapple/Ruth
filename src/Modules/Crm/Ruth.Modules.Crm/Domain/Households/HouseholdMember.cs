using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Domain.People;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Households;

/// <summary>Represents a person's membership and role inside a <see cref="Household" />.</summary>
public sealed class HouseholdMember : Entity<HouseholdMemberId>, IAuditableEntity
{
    private HouseholdMember() { }

    internal HouseholdMember(
        HouseholdMemberId id,
        HouseholdId householdId,
        PersonId personId,
        HouseholdRole role,
        int householdOrder,
        bool isCombinedContributionMember,
        bool isPrimaryContact
    )
        : base(id)
    {
        HouseholdId = householdId;
        PersonId = personId;
        Role = role;
        HouseholdOrder = Math.Max(0, householdOrder);
        IsCombinedContributionMember = isCombinedContributionMember;
        IsPrimaryContact = isPrimaryContact;
    }

    /// <summary>Gets the owning household identifier.</summary>
    public HouseholdId HouseholdId { get; private set; }

    /// <summary>Gets the member person identifier.</summary>
    public PersonId PersonId { get; private set; }

    /// <summary>Gets the person's role in the household (Adult or Child).</summary>
    public HouseholdRole Role { get; private set; }

    /// <summary>Gets the priority order used when a person belongs to multiple blended households.</summary>
    public int HouseholdOrder { get; private set; }

    /// <summary>Gets whether this member participates in the household's combined contribution or billing unit.</summary>
    public bool IsCombinedContributionMember { get; private set; }

    /// <summary>Gets whether this member is explicitly designated as the household's primary contact.</summary>
    public bool IsPrimaryContact { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    internal void Update(
        HouseholdRole role,
        int householdOrder,
        bool isCombinedContributionMember,
        bool isPrimaryContact
    )
    {
        Role = role;
        HouseholdOrder = Math.Max(0, householdOrder);
        IsCombinedContributionMember = isCombinedContributionMember;
        IsPrimaryContact = isPrimaryContact;
    }

    internal void ClearPrimaryContact()
    {
        IsPrimaryContact = false;
    }

    internal void RepointPerson(PersonId targetPersonId)
    {
        PersonId = targetPersonId;
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
