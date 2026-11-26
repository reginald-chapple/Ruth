using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Configurations;
using Ruth.Modules.CheckIn.Domain.Labels;
using Ruth.Modules.CheckIn.Domain.Sessions;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.CheckIn;

internal static class CheckInEntityTypeCatalog
{
    private static readonly EntityTypeRegistration[] _all =
    [
        Create(
            CheckInSystemGuids.CheckInConfiguration,
            CheckInConfiguration.EntityTypeKeyName,
            nameof(CheckInConfiguration),
            "Check-In Configuration",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CheckInSystemGuids.CheckInArea,
            "Ruth.CheckIn.CheckInArea",
            nameof(CheckInArea),
            "Check-In Area",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            CheckInSystemGuids.CheckInAreaLocation,
            "Ruth.CheckIn.CheckInAreaLocation",
            nameof(CheckInAreaLocation),
            "Check-In Area Location",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            CheckInSystemGuids.AttendanceCheckInSession,
            AttendanceCheckInSession.EntityTypeKeyName,
            nameof(AttendanceCheckInSession),
            "Attendance Check-In Session",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            CheckInSystemGuids.CheckInAttendanceEntry,
            "Ruth.CheckIn.CheckInAttendanceEntry",
            nameof(CheckInAttendanceEntry),
            "Check-In Attendance Entry",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            CheckInSystemGuids.CheckInLabel,
            CheckInLabel.EntityTypeKeyName,
            nameof(CheckInLabel),
            "Check-In Badge & Receipt Label",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CheckInSystemGuids.WalletPassTemplate,
            WalletPassTemplate.EntityTypeKeyName,
            nameof(WalletPassTemplate),
            "Smart Wallet Pass Template",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CheckInSystemGuids.IssuedWalletPass,
            IssuedWalletPass.EntityTypeKeyName,
            nameof(IssuedWalletPass),
            "Issued Smart Wallet Pass",
            isSecured: false,
            supportsAttributes: false
        ),
    ];

    public static EntityTypeRegistration[] GetAll()
    {
        return _all;
    }

    private static EntityTypeRegistration Create(
        Guid systemGuid,
        string key,
        string name,
        string friendlyName,
        bool isSecured,
        bool supportsAttributes
    )
    {
        return new EntityTypeRegistration(
            systemGuid,
            key,
            name,
            friendlyName,
            "CheckIn",
            IsEntity: true,
            IsSecured: isSecured,
            SupportsAttributes: supportsAttributes
        );
    }
}
