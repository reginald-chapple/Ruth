namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Complete evaluated seat allocation and pricing plan for a Registration submission or preview.</summary>
public sealed record EvaluatedRegistrationPlan(
    int ConfirmedCount,
    int WaitlistedCount,
    decimal BaseSubtotal,
    decimal FeesSubtotal,
    decimal DiscountTotal,
    string? AppliedDiscountCode,
    decimal TotalAmount,
    decimal MinimumInitialAmountDue,
    IReadOnlyList<EvaluatedRegistrantPlan> Registrants
);
