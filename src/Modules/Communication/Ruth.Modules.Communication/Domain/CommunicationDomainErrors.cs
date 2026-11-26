using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Domain;

/// <summary>Domain errors for communication transports, system notifications, phone numbers, and personal devices.</summary>
public static class CommunicationDomainErrors
{
    public static readonly DomainError SystemCommunicationTitleInvalid = DomainError.Validation(
        "Communication.SystemCommunicationTitleInvalid",
        "System communication title is required and cannot exceed 150 characters."
    );

    public static readonly DomainError SystemCommunicationKeyInvalid = DomainError.Validation(
        "Communication.SystemCommunicationKeyInvalid",
        "System communication key must be 3–100 lowercase letters, digits, dots, hyphens, or underscores starting with a letter."
    );

    public static readonly DomainError SystemCommunicationKeyDuplicate = DomainError.Conflict(
        "Communication.SystemCommunicationKeyDuplicate",
        "A system communication with the specified key already exists."
    );

    public static readonly DomainError SystemCommunicationSubjectInvalid = DomainError.Validation(
        "Communication.SystemCommunicationSubjectInvalid",
        "System communication subject template is required and cannot exceed 500 characters."
    );

    public static readonly DomainError SystemCommunicationBodyInvalid = DomainError.Validation(
        "Communication.SystemCommunicationBodyInvalid",
        "System communication HTML body template is required and cannot exceed 131,072 characters."
    );

    public static readonly DomainError SystemCommunicationEmailInvalid = DomainError.Validation(
        "Communication.SystemCommunicationEmailInvalid",
        "Sender email address override must be a valid email address."
    );

    public static readonly DomainError SystemCommunicationNotFound = DomainError.NotFound(
        "Communication.SystemCommunicationNotFound",
        "System communication template was not found."
    );

    public static readonly DomainError SystemCommunicationInactive = DomainError.Conflict(
        "Communication.SystemCommunicationInactive",
        "System communication template is currently inactive."
    );

    public static readonly DomainError BuiltInSystemCommunicationProtected = DomainError.Conflict(
        "Communication.BuiltInSystemCommunicationProtected",
        "Built-in system communication templates cannot be deleted."
    );

    public static readonly DomainError SystemPhoneNameInvalid = DomainError.Validation(
        "Communication.SystemPhoneNameInvalid",
        "System phone number name is required and cannot exceed 120 characters."
    );

    public static readonly DomainError SystemPhoneNumberInvalid = DomainError.Validation(
        "Communication.SystemPhoneNumberInvalid",
        "Phone number must normalize to a valid E.164 number (e.g., +15552345678)."
    );

    public static readonly DomainError SystemPhoneNumberDuplicate = DomainError.Conflict(
        "Communication.SystemPhoneNumberDuplicate",
        "A system phone number with this E.164 number is already registered."
    );

    public static readonly DomainError SystemPhoneNumberNotFound = DomainError.NotFound(
        "Communication.SystemPhoneNumberNotFound",
        "System phone number was not found."
    );

    public static readonly DomainError SystemPhoneNumberInUse = DomainError.Conflict(
        "Communication.SystemPhoneNumberInUse",
        "System phone number is referenced by one or more system communications and cannot be deleted."
    );

    public static readonly DomainError PersonalDeviceTokenInvalid = DomainError.Validation(
        "Communication.PersonalDeviceTokenInvalid",
        "Push registration token must be between 8 and 2,048 non-whitespace characters."
    );

    public static readonly DomainError PersonalDeviceNotFound = DomainError.NotFound(
        "Communication.PersonalDeviceNotFound",
        "Personal device registration was not found."
    );

    public static readonly DomainError TransportRecipientRequired = DomainError.Validation(
        "Communication.TransportRecipientRequired",
        "Outbound message destination address is required."
    );

    public static DomainError TransportNotConfigured(string transportKey)
    {
        return DomainError.Conflict(
            "Communication.TransportNotConfigured",
            $"Communication transport '{transportKey}' is not configured with required credentials or endpoints."
        );
    }

    public static DomainError TransportDeliveryFailed(string transportKey, string detail)
    {
        return DomainError.Failure(
            "Communication.TransportDeliveryFailed",
            $"Transport '{transportKey}' failed to deliver message: {detail}"
        );
    }
}
