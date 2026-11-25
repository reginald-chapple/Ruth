using Ruth.Modules.Core.Contracts.Branches;
using Ruth.Modules.Core.Contracts.Devices;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Core.Domain;
using Xunit;

namespace Ruth.Modules.Core.Tests.Domain;

public sealed class CoreLocationsAndDevicesDomainTests
{
    [Fact]
    public void Branch_Enforces_Validation_CondensedName_And_LocalTime()
    {
        var created = Branch.Create(
            new BranchInput(
                "Main Branch",
                "Primary site",
                ShortCode: null,
                IsActive: true,
                Url: "https://civic.example.org",
                PhoneNumber: "602-555-0100",
                TimeZoneId: "America/Denver",
                LocationId: null,
                LeaderPersonId: null,
                BranchStatusValueId: null,
                BranchTypeValueId: null,
                PhotoId: null,
                OpenedDate: new DateOnly(2020, 1, 1),
                ClosedDate: null,
                BeaconId: 42,
                Order: 0,
                Schedules:
                [
                    new BranchScheduleDto(Guid.CreateVersion7(), ScheduleTypeValueId: null, 0),
                ],
                Topics:
                [
                    new BranchTopicDto(
                        Guid.CreateVersion7(),
                        "info@civic.example.org",
                        IsPublic: true
                    ),
                ]
            )
        );

        Assert.True(created.IsSuccess);
        Assert.Equal("Main", created.Value.CondensedName);
        _ = Assert.Single(created.Value.Schedules);
        _ = Assert.Single(created.Value.Topics);
        var summerUtc = new DateTimeOffset(2026, 7, 1, 18, 0, 0, TimeSpan.Zero);
        Assert.Equal(TimeSpan.FromHours(-6), created.Value.GetCurrentLocalTime(summerUtc).Offset);

        var siteNamed = Branch.Create(
            new BranchInput(
                "West Site",
                Description: null,
                ShortCode: null,
                IsActive: true,
                Url: null,
                PhoneNumber: null,
                TimeZoneId: null,
                LocationId: null,
                LeaderPersonId: null,
                BranchStatusValueId: null,
                BranchTypeValueId: null,
                PhotoId: null,
                OpenedDate: null,
                ClosedDate: null,
                BeaconId: null,
                Order: 1,
                Schedules: null,
                Topics: null
            )
        );
        Assert.True(siteNamed.IsSuccess);
        Assert.Equal("West", siteNamed.Value.CondensedName);

        var badUrl = Branch.Create(
            new BranchInput(
                "Bad",
                Description: null,
                ShortCode: null,
                IsActive: true,
                "ftp://bad",
                PhoneNumber: null,
                TimeZoneId: null,
                LocationId: null,
                LeaderPersonId: null,
                BranchStatusValueId: null,
                BranchTypeValueId: null,
                PhotoId: null,
                OpenedDate: null,
                ClosedDate: null,
                BeaconId: null,
                0,
                Schedules: null,
                Topics: null
            )
        );
        Assert.True(badUrl.IsFailure);
        Assert.Equal(CoreLocationErrors.BranchUrlInvalid.Code, badUrl.PrimaryError.Code);

        var badDates = Branch.Create(
            new BranchInput(
                "Dates",
                Description: null,
                ShortCode: null,
                IsActive: true,
                Url: null,
                PhoneNumber: null,
                TimeZoneId: null,
                LocationId: null,
                LeaderPersonId: null,
                BranchStatusValueId: null,
                BranchTypeValueId: null,
                PhotoId: null,
                new DateOnly(2025, 5, 1),
                new DateOnly(2025, 4, 1),
                BeaconId: null,
                0,
                Schedules: null,
                Topics: null
            )
        );
        Assert.True(badDates.IsFailure);
        Assert.Equal(CoreLocationErrors.BranchDateRangeInvalid.Code, badDates.PrimaryError.Code);
    }

    [Fact]
    public void Location_Validates_GeoGeometry_Thresholds_And_Cycle_Prevention()
    {
        var invalidPoint = GeoGeometryHelper.CreatePoint(new GeoPointDto(95.0, -112.0));
        Assert.True(invalidPoint.IsFailure);

        var validPolygon = GeoGeometryHelper.CreatePolygon([
            new GeoPointDto(33.58, -112.02),
            new GeoPointDto(33.58, -112.00),
            new GeoPointDto(33.60, -112.00),
            new GeoPointDto(33.60, -112.02),
        ]);
        Assert.True(validPolygon.IsSuccess);
        Assert.NotNull(validPolygon.Value);
        Assert.Equal(GeoGeometryHelper.Wgs84Srid, validPolygon.Value.SRID);

        var badThresholds = Location.Create(
            new LocationInput(
                "Room 101",
                ParentLocationId: null,
                Description: null,
                IsActive: true,
                LocationTypeValueId: null,
                ImageId: null,
                Address: null,
                GeoPoint: null,
                GeoFenceRing: null,
                IsGeoPointLocked: false,
                Barcode: null,
                AssessorParcelId: null,
                PrinterDeviceId: null,
                SoftRoomThreshold: 50,
                FirmRoomThreshold: 30,
                BeaconId: null
            )
        );
        Assert.True(badThresholds.IsFailure);
        Assert.Equal(
            CoreLocationErrors.LocationThresholdsInvalid.Code,
            badThresholds.PrimaryError.Code
        );

        var root = LocationId.Create();
        var child = LocationId.Create();
        var grandChild = LocationId.Create();
        var parents = new Dictionary<LocationId, LocationId?>
        {
            [root] = null,
            [child] = root,
            [grandChild] = child,
        };

        Assert.True(LocationTreeValidator.ValidateParentMove(root, root, parents).IsFailure);
        Assert.True(LocationTreeValidator.ValidateParentMove(root, grandChild, parents).IsFailure);
        Assert.True(LocationTreeValidator.ValidateParentMove(grandChild, root, parents).IsSuccess);
    }

    [Fact]
    public void Location_Verification_Respects_GeoLock_And_ConnectionError_Retry()
    {
        var loc = Location
            .Create(
                new LocationInput(
                    Name: null,
                    ParentLocationId: null,
                    Description: null,
                    IsActive: true,
                    LocationTypeValueId: null,
                    ImageId: null,
                    new LocationAddressDto(
                        "123 Main St",
                        Street2: null,
                        "Phoenix",
                        County: null,
                        "AZ",
                        "85001",
                        "US"
                    ),
                    new GeoPointDto(33.45, -112.07),
                    GeoFenceRing: null,
                    IsGeoPointLocked: true,
                    Barcode: null,
                    AssessorParcelId: null,
                    PrinterDeviceId: null,
                    SoftRoomThreshold: null,
                    FirmRoomThreshold: null,
                    BeaconId: null
                )
            )
            .Value;

        var now = DateTimeOffset.UtcNow;
        loc.ApplyVerificationOutcome(
            new AddressVerificationOutcome(
                AddressVerificationStatus.ConnectionError,
                "Mock",
                "Timeout"
            ),
            now,
            allowStandardization: true,
            allowGeocoding: true
        );
        Assert.Null(loc.StandardizeAttemptedOnUtc);
        Assert.Null(loc.GeocodeAttemptedOnUtc);

        loc.ApplyVerificationOutcome(
            new AddressVerificationOutcome(
                AddressVerificationStatus.Standardized | AddressVerificationStatus.Geocoded,
                "Mock",
                "OK",
                new LocationAddressDto(
                    "123 MAIN ST",
                    Street2: null,
                    "PHOENIX",
                    County: null,
                    "AZ",
                    "85001-1234",
                    "US"
                ),
                new GeoPointDto(34.0, -111.0),
                "85001123499"
            ),
            now,
            allowStandardization: true,
            allowGeocoding: true
        );

        Assert.Equal("123 MAIN ST", loc.Street1);
        Assert.Equal(now, loc.StandardizedOnUtc);
        Assert.Equal(33.45, loc.GeoPoint!.Y, precision: 4);
        Assert.Null(loc.GeocodedOnUtc);

        // Unlocked location updates GeoPoint and validates negative thresholds / bad beacon
        var unlocked = Location
            .Create(
                new LocationInput(
                    "Room B",
                    ParentLocationId: null,
                    Description: null,
                    IsActive: true,
                    LocationTypeValueId: null,
                    ImageId: null,
                    Address: null,
                    GeoPoint: null,
                    GeoFenceRing: null,
                    IsGeoPointLocked: false,
                    Barcode: null,
                    AssessorParcelId: null,
                    PrinterDeviceId: null,
                    SoftRoomThreshold: 10,
                    FirmRoomThreshold: 20,
                    BeaconId: 1
                )
            )
            .Value;
        unlocked.ApplyVerificationOutcome(
            new AddressVerificationOutcome(
                AddressVerificationStatus.Geocoded,
                "Geo",
                "Found",
                StandardizedAddress: null,
                new GeoPointDto(33.5, -112.1)
            ),
            now,
            allowStandardization: false,
            allowGeocoding: true
        );
        _ = Assert.NotNull(unlocked.GeocodedOnUtc);
        Assert.Equal(33.5, unlocked.GeoPoint!.Y, precision: 4);

        Assert.True(
            Location
                .Create(
                    new LocationInput(
                        "Bad",
                        ParentLocationId: null,
                        Description: null,
                        IsActive: true,
                        LocationTypeValueId: null,
                        ImageId: null,
                        Address: null,
                        GeoPoint: null,
                        GeoFenceRing: null,
                        IsGeoPointLocked: false,
                        Barcode: null,
                        AssessorParcelId: null,
                        PrinterDeviceId: null,
                        -1,
                        10,
                        BeaconId: null
                    )
                )
                .IsFailure
        );
        Assert.True(
            Location
                .Create(
                    new LocationInput(
                        "Bad",
                        ParentLocationId: null,
                        Description: null,
                        IsActive: true,
                        LocationTypeValueId: null,
                        ImageId: null,
                        Address: null,
                        GeoPoint: null,
                        GeoFenceRing: null,
                        IsGeoPointLocked: false,
                        Barcode: null,
                        AssessorParcelId: null,
                        PrinterDeviceId: null,
                        SoftRoomThreshold: null,
                        -1,
                        BeaconId: null
                    )
                )
                .IsFailure
        );
        Assert.True(
            Location
                .Create(
                    new LocationInput(
                        "Bad",
                        ParentLocationId: null,
                        Description: null,
                        IsActive: true,
                        LocationTypeValueId: null,
                        ImageId: null,
                        Address: null,
                        GeoPoint: null,
                        GeoFenceRing: null,
                        IsGeoPointLocked: false,
                        Barcode: null,
                        AssessorParcelId: null,
                        PrinterDeviceId: null,
                        SoftRoomThreshold: null,
                        FirmRoomThreshold: null,
                        70000
                    )
                )
                .IsFailure
        );
    }

    [Fact]
    public void Device_Validates_Name_Type_And_Distinct_AssignedLocations()
    {
        var loc1 = LocationId.Create();
        var created = Device.Create(
            new DeviceInput(
                "Lobby Kiosk 1",
                Guid.CreateVersion7(),
                "Check-in tablet",
                "10.0.0.15",
                IsActive: true,
                loc1,
                DevicePrintFrom.Server,
                DevicePrintToOverride.Kiosk,
                PrinterDeviceId: null,
                ProxyDeviceId: null,
                HasCamera: true,
                CameraBarcodeConfiguration.Available,
                KioskType.IPad,
                [loc1, loc1]
            )
        );
        Assert.True(created.IsSuccess);
        _ = Assert.Single(created.Value.Locations);

        var missingType = Device.Create(
            new DeviceInput(
                "Bad",
                Guid.Empty,
                Description: null,
                IpAddress: null,
                IsActive: true,
                LocationId: null,
                DevicePrintFrom.Client,
                DevicePrintToOverride.Default,
                PrinterDeviceId: null,
                ProxyDeviceId: null,
                HasCamera: false,
                CameraBarcodeConfigurationType: null,
                KioskType: null,
                AssignedLocationIds: null
            )
        );
        Assert.True(missingType.IsFailure);
    }
}
