using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Domain;

/// <summary>Domain error catalog for the CheckIn bounded context.</summary>
public static class CheckInErrors
{
    /// <summary>Configuration name is required.</summary>
    public static readonly DomainError ConfigurationNameRequired = DomainError.Validation(
        "CheckIn.ConfigurationNameRequired",
        "Check-in configuration name is required."
    );

    /// <summary>Configuration name exceeds maximum length.</summary>
    public static readonly DomainError ConfigurationNameTooLong = DomainError.Validation(
        "CheckIn.ConfigurationNameTooLong",
        "Check-in configuration name must not exceed 120 characters."
    );

    /// <summary>Description exceeds maximum length.</summary>
    public static readonly DomainError DescriptionTooLong = DomainError.Validation(
        "CheckIn.DescriptionTooLong",
        "Description must not exceed 2000 characters."
    );

    /// <summary>Text contains prohibited control characters.</summary>
    public static readonly DomainError InvalidControlCharacters = DomainError.Validation(
        "CheckIn.InvalidControlCharacters",
        "Text fields must not contain NUL bytes or control characters."
    );

    /// <summary>Configuration profile was not found.</summary>
    public static readonly DomainError ConfigurationNotFound = DomainError.NotFound(
        "CheckIn.ConfigurationNotFound",
        "The requested check-in configuration profile was not found."
    );

    /// <summary>Duplicate configuration name.</summary>
    public static readonly DomainError ConfigurationDuplicateName = DomainError.Conflict(
        "CheckIn.ConfigurationDuplicateName",
        "A check-in configuration profile with the same name already exists."
    );

    /// <summary>Invalid check-in start/end window offsets.</summary>
    public static readonly DomainError InvalidCheckInWindowOffsets = DomainError.Validation(
        "CheckIn.InvalidCheckInWindowOffsets",
        "Check-in start offset must be 0..720 minutes, end offset -120..720 minutes, and the total window must be positive."
    );

    /// <summary>Invalid security code character counts.</summary>
    public static readonly DomainError InvalidSecurityCodeLength = DomainError.Validation(
        "CheckIn.InvalidSecurityCodeLength",
        "Security code alpha and numeric lengths must each be between 0 and 6, and at most 8 characters combined."
    );

    /// <summary>Pickup security code requires at least 3 characters.</summary>
    public static readonly DomainError SecurityCodeTooShortForPickup = DomainError.Validation(
        "CheckIn.SecurityCodeTooShortForPickup",
        "Configurations or areas issuing guardian pickup codes require at least 3 combined security code characters."
    );

    /// <summary>Invalid auto-select lookback window.</summary>
    public static readonly DomainError InvalidAutoSelectDaysBack = DomainError.Validation(
        "CheckIn.InvalidAutoSelectDaysBack",
        "Auto-select lookback window must be between 0 and 365 days."
    );

    /// <summary>Area name is required.</summary>
    public static readonly DomainError AreaNameRequired = DomainError.Validation(
        "CheckIn.AreaNameRequired",
        "Check-in area name is required."
    );

    /// <summary>Area name exceeds maximum length.</summary>
    public static readonly DomainError AreaNameTooLong = DomainError.Validation(
        "CheckIn.AreaNameTooLong",
        "Check-in area name must not exceed 120 characters."
    );

    /// <summary>Duplicate area name within a configuration.</summary>
    public static readonly DomainError AreaDuplicateName = DomainError.Conflict(
        "CheckIn.AreaDuplicateName",
        "An area with the same name already exists in this check-in configuration."
    );

    /// <summary>Area was not found in the configuration.</summary>
    public static readonly DomainError AreaNotFound = DomainError.NotFound(
        "CheckIn.AreaNotFound",
        "The requested check-in area was not found on this configuration."
    );

    /// <summary>Enrollment-restricted area requires a linked group.</summary>
    public static readonly DomainError PreEnrolledRequiresLinkedGroup = DomainError.Validation(
        "CheckIn.PreEnrolledRequiresLinkedGroup",
        "Areas requiring pre-enrollment or scheduled shifts must specify a linked GroupId."
    );

    /// <summary>Invalid age range bounds.</summary>
    public static readonly DomainError InvalidAgeRange = DomainError.Validation(
        "CheckIn.InvalidAgeRange",
        "Minimum and maximum age years must be non-negative, at most 130, and MinAgeYears <= MaxAgeYears."
    );

    /// <summary>Invalid birth date range bounds.</summary>
    public static readonly DomainError InvalidBirthDateRange = DomainError.Validation(
        "CheckIn.InvalidBirthDateRange",
        "Minimum birth date must be on or before maximum birth date."
    );

    /// <summary>Invalid grade range bounds.</summary>
    public static readonly DomainError InvalidGradeRange = DomainError.Validation(
        "CheckIn.InvalidGradeRange",
        "Grade bounds must be between -2 and 20, with MinGrade <= MaxGrade."
    );

    /// <summary>Invalid membership tier or skill badge code token.</summary>
    public static readonly DomainError TierOrCodeInvalid = DomainError.Validation(
        "CheckIn.TierOrCodeInvalid",
        "Membership tier and skill/badge codes must be 1..80 printable characters."
    );

    /// <summary>Location identifier is required.</summary>
    public static readonly DomainError LocationRequired = DomainError.Validation(
        "CheckIn.LocationRequired",
        "A valid non-empty LocationId is required for every room matrix entry."
    );

    /// <summary>Location display name is required.</summary>
    public static readonly DomainError LocationNameRequired = DomainError.Validation(
        "CheckIn.LocationNameRequired",
        "Room/location display name is required."
    );

    /// <summary>Location display name exceeds maximum length.</summary>
    public static readonly DomainError LocationNameTooLong = DomainError.Validation(
        "CheckIn.LocationNameTooLong",
        "Room/location display name must not exceed 120 characters."
    );

    /// <summary>Invalid soft/firm room capacities.</summary>
    public static readonly DomainError InvalidRoomCapacities = DomainError.Validation(
        "CheckIn.InvalidRoomCapacities",
        "Soft and firm room capacities must be non-negative, and SoftCapacity must not exceed FirmCapacity."
    );

    /// <summary>Duplicate location within the same area matrix.</summary>
    public static readonly DomainError DuplicateAreaLocation = DomainError.Conflict(
        "CheckIn.DuplicateAreaLocation",
        "Each physical location may appear at most once per check-in area matrix."
    );

    /// <summary>Room matrix entry was not found.</summary>
    public static readonly DomainError RoomNotFound = DomainError.NotFound(
        "CheckIn.RoomNotFound",
        "The requested room/location was not found in the specified check-in area."
    );

    /// <summary>Configuration is inactive.</summary>
    public static readonly DomainError ConfigurationInactive = DomainError.Validation(
        "CheckIn.ConfigurationInactive",
        "This check-in configuration profile is currently inactive."
    );

    /// <summary>Check-in session requires at least one participant item.</summary>
    public static readonly DomainError SessionItemsRequired = DomainError.Validation(
        "CheckIn.SessionItemsRequired",
        "At least one participant selection is required to submit a check-in session."
    );

    /// <summary>Person identifier is required.</summary>
    public static readonly DomainError PersonRequired = DomainError.Validation(
        "CheckIn.PersonRequired",
        "A valid non-empty PersonId is required for check-in."
    );

    /// <summary>Schedule identifier is required.</summary>
    public static readonly DomainError ScheduleRequired = DomainError.Validation(
        "CheckIn.ScheduleRequired",
        "A valid non-empty ScheduleId is required for check-in."
    );

    /// <summary>Search input violates the configuration's allowed search mode.</summary>
    public static readonly DomainError SearchModeNotPermitted = DomainError.Validation(
        "CheckIn.SearchModeNotPermitted",
        "The submitted search input is not permitted by this check-in configuration's search mode."
    );

    /// <summary>Participant does not meet area eligibility rules.</summary>
    public static readonly DomainError ParticipantNotEligible = DomainError.Validation(
        "CheckIn.ParticipantNotEligible",
        "The participant does not satisfy the eligibility requirements for the selected check-in area."
    );

    /// <summary>No open room with remaining capacity is available.</summary>
    public static readonly DomainError NoAvailableRoomForCheckIn = DomainError.Conflict(
        "CheckIn.NoAvailableRoomForCheckIn",
        "All rooms in the selected check-in area are closed or have reached firm capacity."
    );

    /// <summary>Staff override is only allowed on the StaffOverride channel with a non-empty reason.</summary>
    public static readonly DomainError OverrideRequiresStaffAndReason = DomainError.Validation(
        "CheckIn.OverrideRequiresStaffAndReason",
        "Capacity or eligibility overrides require the StaffOverride channel and an explicit audit reason."
    );

    /// <summary>Duplicate check-in prevented for the same participant and schedule.</summary>
    public static readonly DomainError DuplicateActiveCheckIn = DomainError.Conflict(
        "CheckIn.DuplicateActiveCheckIn",
        "The participant is already checked in for this schedule and service date."
    );

    /// <summary>Unable to allocate a collision-free security pickup code after exhausting retries.</summary>
    public static readonly DomainError SecurityCodePoolExhausted = DomainError.Conflict(
        "CheckIn.SecurityCodePoolExhausted",
        "Unable to generate a unique profanity-safe security pickup code for today's pool."
    );

    /// <summary>Check-in session was not found.</summary>
    public static readonly DomainError SessionNotFound = DomainError.NotFound(
        "CheckIn.SessionNotFound",
        "The requested check-in session was not found."
    );

    /// <summary>Check-in attendance entry was not found.</summary>
    public static readonly DomainError EntryNotFound = DomainError.NotFound(
        "CheckIn.EntryNotFound",
        "The requested check-in attendance entry was not found."
    );

    /// <summary>Check-out is disabled on this configuration.</summary>
    public static readonly DomainError CheckOutNotAllowed = DomainError.Validation(
        "CheckIn.CheckOutNotAllowed",
        "Check-out tracking is disabled on this check-in configuration."
    );

    /// <summary>Security pickup code did not match.</summary>
    public static readonly DomainError InvalidSecurityPickupCode = DomainError.Validation(
        "CheckIn.InvalidSecurityPickupCode",
        "A valid matching guardian security pickup code is required to complete check-out."
    );

    /// <summary>Entry is no longer actively checked in.</summary>
    public static readonly DomainError EntryNotActivelyCheckedIn = DomainError.Conflict(
        "CheckIn.EntryNotActivelyCheckedIn",
        "Only actively checked-in entries can be checked out or reassigned."
    );

    /// <summary>Label name is required.</summary>
    public static readonly DomainError LabelNameRequired = DomainError.Validation(
        "CheckIn.LabelNameRequired",
        "Check-in label name is required."
    );

    /// <summary>Label name exceeds maximum length.</summary>
    public static readonly DomainError LabelNameTooLong = DomainError.Validation(
        "CheckIn.LabelNameTooLong",
        "Check-in label name must not exceed 120 characters."
    );

    /// <summary>Label key slug is invalid.</summary>
    public static readonly DomainError LabelKeyInvalid = DomainError.Validation(
        "CheckIn.LabelKeyInvalid",
        "Label key must be 2..80 lowercase letters, digits, or hyphens."
    );

    /// <summary>Duplicate label key.</summary>
    public static readonly DomainError LabelDuplicateKey = DomainError.Conflict(
        "CheckIn.LabelDuplicateKey",
        "A check-in label with the same key already exists."
    );

    /// <summary>Label was not found.</summary>
    public static readonly DomainError LabelNotFound = DomainError.NotFound(
        "CheckIn.LabelNotFound",
        "The requested check-in label was not found."
    );

    /// <summary>System labels cannot be deleted.</summary>
    public static readonly DomainError SystemLabelCannotBeDeleted = DomainError.Conflict(
        "CheckIn.SystemLabelCannotBeDeleted",
        "Built-in system check-in labels cannot be deleted."
    );

    /// <summary>Invalid label layout dimensions or elements.</summary>
    public static readonly DomainError InvalidLabelLayout = DomainError.Validation(
        "CheckIn.InvalidLabelLayout",
        "Label dimensions must be 1.0..6.0\" wide by 0.5..8.0\" tall with 1..50 valid elements."
    );

    /// <summary>Invalid copy count.</summary>
    public static readonly DomainError InvalidLabelCopies = DomainError.Validation(
        "CheckIn.InvalidLabelCopies",
        "Label copies must be between 1 and 5."
    );

    /// <summary>Printer TCP dispatch failed or invalid endpoint.</summary>
    public static readonly DomainError PrinterDispatchFailed = DomainError.Validation(
        "CheckIn.PrinterDispatchFailed",
        "Unable to dispatch ZPL payload to the target Zebra printer endpoint."
    );
}
