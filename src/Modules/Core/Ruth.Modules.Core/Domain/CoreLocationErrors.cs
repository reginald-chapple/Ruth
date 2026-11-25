using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain;

internal static class CoreLocationErrors
{
    public static readonly DomainError BranchNameRequired = DomainError.Validation(
        "Core.BranchNameRequired",
        "Branch name is required."
    );

    public static readonly DomainError BranchNameTooLong = DomainError.Validation(
        "Core.BranchNameTooLong",
        "Branch name cannot exceed 100 characters."
    );

    public static readonly DomainError BranchDuplicateName = DomainError.Conflict(
        "Core.BranchDuplicateName",
        "A branch with that name already exists."
    );

    public static readonly DomainError BranchNotFound = DomainError.NotFound(
        "Core.BranchNotFound",
        "Branch was not found."
    );

    public static readonly DomainError BranchLastCannotDelete = DomainError.Conflict(
        "Core.BranchLastCannotDelete",
        "Cannot delete the last remaining branch."
    );

    public static readonly DomainError BranchUrlInvalid = DomainError.Validation(
        "Core.BranchUrlInvalid",
        "Branch URL must be an absolute HTTP or HTTPS URI."
    );

    public static readonly DomainError BranchTimeZoneInvalid = DomainError.Validation(
        "Core.BranchTimeZoneInvalid",
        "Branch time zone identifier is not recognized."
    );

    public static readonly DomainError BranchDateRangeInvalid = DomainError.Validation(
        "Core.BranchDateRangeInvalid",
        "Branch closed date cannot be earlier than opened date."
    );

    public static readonly DomainError BranchTopicEmailInvalid = DomainError.Validation(
        "Core.BranchTopicEmailInvalid",
        "Branch topic email must be a valid email address."
    );

    public static readonly DomainError BranchBeaconInvalid = DomainError.Validation(
        "Core.BranchBeaconInvalid",
        "Beacon identifier must be between 0 and 65535."
    );

    public static readonly DomainError LocationNotFound = DomainError.NotFound(
        "Core.LocationNotFound",
        "Location was not found."
    );

    public static readonly DomainError LocationEmpty = DomainError.Validation(
        "Core.LocationEmpty",
        "A location must have a name, street address, geo point, or geofence polygon."
    );

    public static readonly DomainError LocationNameTooLong = DomainError.Validation(
        "Core.LocationNameTooLong",
        "Location name cannot exceed 100 characters."
    );

    public static readonly DomainError LocationCycleDetected = DomainError.Validation(
        "Core.LocationCycleDetected",
        "Moving this location would create a circular parent-child hierarchy."
    );

    public static readonly DomainError LocationHasChildren = DomainError.Conflict(
        "Core.LocationHasChildren",
        "Cannot delete a location that still has child locations."
    );

    public static readonly DomainError LocationGeoPointInvalid = DomainError.Validation(
        "Core.LocationGeoPointInvalid",
        "Geo coordinates must have latitude in [-90, 90] and longitude in [-180, 180]."
    );

    public static readonly DomainError LocationGeoFenceInvalid = DomainError.Validation(
        "Core.LocationGeoFenceInvalid",
        "Geofence polygon must contain at least 3 distinct points with valid WGS84 coordinates."
    );

    public static readonly DomainError LocationThresholdsInvalid = DomainError.Validation(
        "Core.LocationThresholdsInvalid",
        "Room thresholds must be non-negative and soft threshold cannot exceed firm threshold."
    );

    public static readonly DomainError RadiusInvalid = DomainError.Validation(
        "Core.RadiusInvalid",
        "Search radius must be positive and at most 20,000,000 meters."
    );

    public static readonly DomainError DeviceNameRequired = DomainError.Validation(
        "Core.DeviceNameRequired",
        "Device name is required."
    );

    public static readonly DomainError DeviceNameTooLong = DomainError.Validation(
        "Core.DeviceNameTooLong",
        "Device name cannot exceed 50 characters."
    );

    public static readonly DomainError DeviceTypeRequired = DomainError.Validation(
        "Core.DeviceTypeRequired",
        "Device type is required."
    );

    public static readonly DomainError DeviceNotFound = DomainError.NotFound(
        "Core.DeviceNotFound",
        "Device was not found."
    );

    public static readonly DomainError DeviceDuplicateName = DomainError.Conflict(
        "Core.DeviceDuplicateName",
        "A device with that name already exists."
    );

    public static readonly DomainError DeviceDuplicateIp = DomainError.Conflict(
        "Core.DeviceDuplicateIp",
        "A device of this type with that IP address already exists."
    );

    public static readonly DomainError DeviceIpTooLong = DomainError.Validation(
        "Core.DeviceIpTooLong",
        "Device IP address or hostname cannot exceed 45 characters."
    );
}
