using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Domain errors for event registration templates, instances, seat holds, pricing, and waitlists.</summary>
public static class EventsRegistrationErrors
{
    public static readonly DomainError TemplateInvalid = DomainError.Validation(
        "Events.RegistrationTemplate.Invalid",
        "Registration template requires a valid name, financial account, 3-letter currency, non-negative cost/deposit, and valid forms/fees/discounts."
    );

    public static readonly DomainError TemplateNotFound = DomainError.NotFound(
        "Events.RegistrationTemplate.NotFound",
        "The requested registration template was not found."
    );

    public static readonly DomainError InstanceInvalid = DomainError.Validation(
        "Events.RegistrationInstance.Invalid",
        "Registration instance requires a valid name, URL slug, positive max attendees when capped, and valid date window."
    );

    public static readonly DomainError InstanceSlugConflict = DomainError.Conflict(
        "Events.RegistrationInstance.SlugConflict",
        "Another registration instance already uses this URL slug."
    );

    public static readonly DomainError InstanceNotFound = DomainError.NotFound(
        "Events.RegistrationInstance.NotFound",
        "The requested registration instance was not found."
    );

    public static readonly DomainError InstanceClosed = DomainError.Conflict(
        "Events.RegistrationInstance.Closed",
        "This registration instance is not currently open for registration."
    );

    public static readonly DomainError CapacityFull = DomainError.Conflict(
        "Events.Registration.CapacityFull",
        "Insufficient confirmed seats remain and waitlisting is disabled for this event."
    );

    public static readonly DomainError SessionHoldInvalid = DomainError.Validation(
        "Events.RegistrationSession.Invalid",
        "Requested seat count must be between 1 and the template per-submission limit."
    );

    public static readonly DomainError SessionHoldExpired = DomainError.Conflict(
        "Events.RegistrationSession.Expired",
        "The seat-hold session token has expired or is no longer active."
    );

    public static readonly DomainError FormValidationFailed = DomainError.Validation(
        "Events.Registration.FormValidationFailed",
        "One or more required dynamic form fields or registrant identity fields are missing or invalid."
    );

    public static readonly DomainError FeeSelectionInvalid = DomainError.Validation(
        "Events.Registration.FeeSelectionInvalid",
        "One or more fee selections violate required rules, single-option constraints, or remaining inventory."
    );

    public static readonly DomainError DiscountInvalid = DomainError.Conflict(
        "Events.Registration.DiscountInvalid",
        "The supplied discount code is unknown, outside its active date window, or has reached its usage limit."
    );

    public static readonly DomainError PaymentAmountInvalid = DomainError.Validation(
        "Events.Registration.PaymentAmountInvalid",
        "Payment amount does not satisfy the configured payment model, minimum deposit, or remaining balance."
    );

    public static readonly DomainError RegistrationNotFound = DomainError.NotFound(
        "Events.Registration.NotFound",
        "The requested registration was not found."
    );

    public static readonly DomainError RegistrantNotFound = DomainError.NotFound(
        "Events.Registrant.NotFound",
        "The requested registrant was not found."
    );
}
