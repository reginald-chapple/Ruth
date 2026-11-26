using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Individual attendee within a Registration, tracking confirmed vs waitlisted seat state, dynamic form answers, fees, and group placement.</summary>
public sealed class RegistrationRegistrant : Entity<RegistrationRegistrantId>
{
    private readonly List<RegistrationRegistrantFeeSelection> _feeSelections = [];
    private Dictionary<string, string?> _fieldValues = new(StringComparer.OrdinalIgnoreCase);

    private RegistrationRegistrant() { }

    internal RegistrationRegistrant(
        RegistrationRegistrantId id,
        RegistrationId registrationId,
        RegistrationInstanceId instanceId,
        EvaluatedRegistrantPlan plan,
        DateTimeOffset createdOnUtc
    )
        : base(id)
    {
        RegistrationId = registrationId;
        InstanceId = instanceId;
        PersonId = plan.PersonId is { } p && p != Guid.Empty ? p : null;
        FirstName = plan.FirstName;
        LastName = plan.LastName;
        Email = plan.Email;
        Phone = plan.Phone;
        Status = plan.Status;
        WaitlistPosition = plan.WaitlistPosition;
        BaseCost = plan.BaseCost;
        FeesTotal = plan.FeesTotal;
        DiscountAmount = plan.DiscountAmount;
        NetCost = plan.NetCost;
        FieldValues = plan.FieldValues;
        CreatedOnUtc = createdOnUtc.ToUniversalTime();

        foreach (var fee in plan.FeeSelections)
        {
            _feeSelections.Add(
                new RegistrationRegistrantFeeSelection(
                    RegistrationRegistrantFeeSelectionId.Create(),
                    id,
                    fee.FeeId,
                    fee.FeeItemId,
                    fee.Label,
                    fee.Quantity,
                    fee.UnitAmount
                )
            );
        }
    }

    public RegistrationId RegistrationId { get; private set; }
    public RegistrationInstanceId InstanceId { get; private set; }
    public Guid? PersonId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public RegistrantStatus Status { get; private set; }
    public bool IsOnWaitlist => Status == RegistrantStatus.Waitlisted;
    public int? WaitlistPosition { get; private set; }
    public decimal BaseCost { get; private set; }
    public decimal FeesTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal NetCost { get; private set; }
    public Guid? PlacedGroupId { get; private set; }
    public Guid? PlacedGroupMemberId { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }

    public IReadOnlyDictionary<string, string?> FieldValues
    {
        get => _fieldValues;
        private set =>
            _fieldValues = new Dictionary<string, string?>(value, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<RegistrationRegistrantFeeSelection> FeeSelections =>
        _feeSelections.AsReadOnly();

    public bool Cancel()
    {
        if (Status == RegistrantStatus.Cancelled)
        {
            return false;
        }

        Status = RegistrantStatus.Cancelled;
        WaitlistPosition = null;
        BaseCost = 0m;
        FeesTotal = 0m;
        DiscountAmount = 0m;
        NetCost = 0m;
        return true;
    }

    public bool PromoteFromWaitlist(decimal confirmedBaseCost)
    {
        if (Status != RegistrantStatus.Waitlisted)
        {
            return false;
        }

        Status = RegistrantStatus.Confirmed;
        WaitlistPosition = null;
        BaseCost = decimal.Round(confirmedBaseCost, 2, MidpointRounding.AwayFromZero);
        FeesTotal = decimal.Round(
            _feeSelections.Sum(static s => s.LineTotal),
            2,
            MidpointRounding.AwayFromZero
        );
        NetCost = Math.Max(0m, BaseCost + FeesTotal - DiscountAmount);
        return true;
    }

    public void MarkPlacedInGroup(Guid groupId, Guid groupMemberId)
    {
        PlacedGroupId = groupId;
        PlacedGroupMemberId = groupMemberId;
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId != retiredPersonId)
        {
            return false;
        }

        PersonId = survivingPersonId;
        return true;
    }
}
