using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Domain;

/// <summary>Domain error catalog for the CRM bounded context.</summary>
public static class CrmErrors
{
    public static readonly DomainError InvalidControlCharacters = DomainError.Validation(
        "Crm.InvalidControlCharacters",
        "Text values cannot contain NUL or disallowed control characters."
    );

    public static readonly DomainError TextTooLong = DomainError.Validation(
        "Crm.TextTooLong",
        "The supplied text exceeds the maximum allowed length."
    );

    public static readonly DomainError PersonNotFound = DomainError.NotFound(
        "Crm.Person.NotFound",
        "The requested person record was not found."
    );

    public static readonly DomainError PersonFirstNameRequired = DomainError.Validation(
        "Crm.Person.FirstNameRequired",
        "First name is required for a person record."
    );

    public static readonly DomainError PersonLastNameRequired = DomainError.Validation(
        "Crm.Person.LastNameRequired",
        "Last name is required."
    );

    public static readonly DomainError PersonNameTooLong = DomainError.Validation(
        "Crm.Person.NameTooLong",
        "Name fields cannot exceed 50 characters."
    );

    public static readonly DomainError PersonEmailInvalid = DomainError.Validation(
        "Crm.Person.EmailInvalid",
        "The email address format is invalid or exceeds 75 characters."
    );

    public static readonly DomainError PersonBirthDateInvalid = DomainError.Validation(
        "Crm.Person.BirthDateInvalid",
        "The supplied birth year, month, and day do not form a valid calendar date."
    );

    public static readonly DomainError PersonDeceasedDateInvalid = DomainError.Validation(
        "Crm.Person.DeceasedDateInvalid",
        "Deceased date cannot precede birth date."
    );

    public static readonly DomainError PersonAlreadyMerged = DomainError.Conflict(
        "Crm.Person.AlreadyMerged",
        "This person record has already been merged into another record."
    );

    public static readonly DomainError PhoneNumberRequired = DomainError.Validation(
        "Crm.Phone.NumberRequired",
        "A valid phone number with digits is required."
    );

    public static readonly DomainError PhoneNumberInvalid = DomainError.Validation(
        "Crm.Phone.Invalid",
        "Phone number must contain between 7 and 20 digits and a valid country code."
    );

    public static readonly DomainError PhoneNumberNotFound = DomainError.NotFound(
        "Crm.Phone.NotFound",
        "The requested phone number was not found on this person."
    );

    public static readonly DomainError SearchKeyRequired = DomainError.Validation(
        "Crm.SearchKey.Required",
        "Search key value is required."
    );

    public static readonly DomainError SearchKeyTooLong = DomainError.Validation(
        "Crm.SearchKey.TooLong",
        "Search key value cannot exceed 255 characters."
    );

    public static readonly DomainError SearchKeyDuplicate = DomainError.Conflict(
        "Crm.SearchKey.Duplicate",
        "This search key already exists on the person."
    );

    public static readonly DomainError SearchKeyNotFound = DomainError.NotFound(
        "Crm.SearchKey.NotFound",
        "The requested search key was not found."
    );

    public static readonly DomainError HouseholdNotFound = DomainError.NotFound(
        "Crm.Household.NotFound",
        "The requested household was not found."
    );

    public static readonly DomainError HouseholdNameRequired = DomainError.Validation(
        "Crm.Household.NameRequired",
        "Household name is required."
    );

    public static readonly DomainError HouseholdNameTooLong = DomainError.Validation(
        "Crm.Household.NameTooLong",
        "Household name cannot exceed 100 characters."
    );

    public static readonly DomainError HouseholdMemberAlreadyExists = DomainError.Conflict(
        "Crm.Household.MemberAlreadyExists",
        "The person is already a member of this household."
    );

    public static readonly DomainError HouseholdMemberNotFound = DomainError.NotFound(
        "Crm.Household.MemberNotFound",
        "The person is not a member of this household."
    );

    public static readonly DomainError HouseholdLocationInvalid = DomainError.Validation(
        "Crm.Household.LocationInvalid",
        "A valid Core LocationId is required."
    );

    public static readonly DomainError RelationshipSelfReference = DomainError.Validation(
        "Crm.Relationship.SelfReference",
        "A person cannot have a relationship with themselves."
    );

    public static readonly DomainError RelationshipAlreadyExists = DomainError.Conflict(
        "Crm.Relationship.AlreadyExists",
        "This known relationship already exists."
    );

    public static readonly DomainError RelationshipNotFound = DomainError.NotFound(
        "Crm.Relationship.NotFound",
        "The requested relationship was not found."
    );

    public static readonly DomainError DuplicateNotFound = DomainError.NotFound(
        "Crm.Duplicate.NotFound",
        "The requested person duplicate pair was not found."
    );

    public static readonly DomainError MergeSamePerson = DomainError.Validation(
        "Crm.Merge.SamePerson",
        "Surviving person and retired person must be distinct records."
    );

    public static readonly DomainError MergeProtectionForbidden = DomainError.Forbidden(
        "Crm.Merge.ProtectionForbidden",
        "Merging records with High or Extreme account protection requires elevated merge authorization."
    );

    public static readonly DomainError SignalTypeNameRequired = DomainError.Validation(
        "Crm.SignalType.NameRequired",
        "Signal type name is required."
    );

    public static readonly DomainError SignalTypeColorRequired = DomainError.Validation(
        "Crm.SignalType.ColorRequired",
        "Signal type color is required."
    );

    public static readonly DomainError SignalTypeDuplicate = DomainError.Conflict(
        "Crm.SignalType.Duplicate",
        "A signal type with that name already exists."
    );

    public static readonly DomainError SignalTypeNotFound = DomainError.NotFound(
        "Crm.SignalType.NotFound",
        "The requested signal type was not found."
    );

    public static readonly DomainError PersonSignalNotFound = DomainError.NotFound(
        "Crm.PersonSignal.NotFound",
        "The requested person signal was not found."
    );

    public static readonly DomainError BadgeNameRequired = DomainError.Validation(
        "Crm.Badge.NameRequired",
        "Badge name and component key are required."
    );

    public static readonly DomainError BadgeNotFound = DomainError.NotFound(
        "Crm.Badge.NotFound",
        "The requested badge definition was not found."
    );

    public static readonly DomainError AssessmentTypeNotFound = DomainError.NotFound(
        "Crm.AssessmentType.NotFound",
        "The requested assessment type was not found."
    );

    public static readonly DomainError AssessmentTypeInvalid = DomainError.Validation(
        "Crm.AssessmentType.Invalid",
        "Assessment type key, title, and path are required."
    );

    public static readonly DomainError AssessmentTypeDuplicate = DomainError.Conflict(
        "Crm.AssessmentType.Duplicate",
        "An assessment type with that key already exists."
    );

    public static readonly DomainError AssessmentNotFound = DomainError.NotFound(
        "Crm.Assessment.NotFound",
        "The requested assessment record was not found."
    );

    public static readonly DomainError AssessmentCooldownActive = DomainError.Conflict(
        "Crm.Assessment.CooldownActive",
        "The minimum retake cooldown period has not yet elapsed for this assessment."
    );

    public static readonly DomainError AssessmentRequiresRequest = DomainError.Forbidden(
        "Crm.Assessment.RequiresRequest",
        "This assessment type requires an explicit requester."
    );

    public static readonly DomainError BackgroundCheckProviderNotFound = DomainError.NotFound(
        "Crm.BackgroundCheck.ProviderNotFound",
        "The requested background check provider is not registered."
    );

    public static readonly DomainError BackgroundCheckNotFound = DomainError.NotFound(
        "Crm.BackgroundCheck.NotFound",
        "The requested background check record was not found."
    );

    public static readonly DomainError BackgroundCheckWebhookInvalidSignature =
        DomainError.Unauthorized(
            "Crm.BackgroundCheck.InvalidWebhookSignature",
            "The background check webhook signature is missing, invalid, or outside the replay timestamp window."
        );

    public static readonly DomainError BackgroundCheckWebhookReplayed = DomainError.Unauthorized(
        "Crm.BackgroundCheck.WebhookReplayed",
        "This background check webhook delivery has already been processed in the active replay window."
    );

    public static readonly DomainError IdentityVerificationReferenceRequired =
        DomainError.Validation(
            "Crm.IdentityVerification.ReferenceRequired",
            "A reference number (phone or contact handle) and request IP are required."
        );

    public static readonly DomainError IdentityVerificationIpLimitReached = DomainError.Conflict(
        "Crm.IdentityVerification.IpLimitReached",
        "The daily identity verification request limit has been reached for this IP address."
    );

    public static readonly DomainError IdentityVerificationNotFound = DomainError.NotFound(
        "Crm.IdentityVerification.NotFound",
        "The requested identity verification challenge was not found."
    );

    public static readonly DomainError IdentityVerificationExpiredOrLocked = DomainError.Failure(
        "Crm.IdentityVerification.ExpiredOrLocked",
        "The verification code has expired or exceeded the maximum allowed failed attempts."
    );

    public static readonly DomainError IdentityVerificationCodeMismatch = DomainError.Validation(
        "Crm.IdentityVerification.CodeMismatch",
        "The supplied verification code does not match."
    );
}
