using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Core.Contracts.Campuses;
using Ruth.Modules.Core.Contracts.Devices;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Core.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Core.Tests.Infrastructure;

[Collection(CorePostgresFixtures.Name)]
public sealed class Wave1S5LocationsIntegrationTests(CorePostgresContainerFixture postgresFixture)
{
    [Fact]
    public async Task PostGis_Locations_Geofences_Verification_Work_EndToEnd()
    {
        var cs = await postgresFixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CoreTestHostFactory.BuildAndMigrateAsync(
            cs,
            rustFsEndpoint: null,
            services =>
                _ = services.AddSingleton<IAddressVerificationProvider, FakeAddressProvider>()
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        GeoPointDto[] phoenixRing =
        [
            new(33.5800, -112.0200),
            new(33.5800, -112.0000),
            new(33.6000, -112.0000),
            new(33.6000, -112.0200),
        ];
        var mainBuilding = (
            await commands.SendAsync<CreateLocationCommand, LocationResponse>(
                new CreateLocationCommand(
                    Name: "Main Campus Building",
                    Address: new LocationAddressDto(
                        "3120 E Cholla St",
                        Street2: null,
                        "Phoenix",
                        County: null,
                        "AZ",
                        "85028",
                        "US"
                    ),
                    GeoPoint: new GeoPointDto(33.5898, -112.0132),
                    GeoFenceRing: phoenixRing,
                    SoftRoomThreshold: 40,
                    FirmRoomThreshold: 50
                )
            )
        ).Value;

        var room101 = (
            await commands.SendAsync<CreateLocationCommand, LocationResponse>(
                new CreateLocationCommand(
                    Name: "Kids Room 101",
                    ParentLocationId: mainBuilding.Id,
                    GeoPoint: new GeoPointDto(33.5900, -112.0130),
                    SoftRoomThreshold: 15,
                    FirmRoomThreshold: 20
                )
            )
        ).Value;

        var cycleAttempt = await commands.SendAsync<MoveLocationCommand, LocationResponse>(
            new MoveLocationCommand(mainBuilding.Id, room101.Id)
        );
        Assert.True(cycleAttempt.IsFailure);

        var nearby = (
            await queries.QueryAsync<
                FindLocationsWithinRadiusQuery,
                IReadOnlyList<LocationDistanceResponse>
            >(new FindLocationsWithinRadiusQuery(33.5899, -112.0131, RadiusMeters: 5000))
        ).Value;
        Assert.Equal(2, nearby.Count);
        Assert.True(nearby[0].DistanceMeters < 50);

        var inside = (
            await queries.QueryAsync<
                FindLocationsContainingPointQuery,
                IReadOnlyList<LocationResponse>
            >(new FindLocationsContainingPointQuery(33.5900, -112.0100))
        ).Value;
        var matchedInside = Assert.Single(inside);
        Assert.Equal(mainBuilding.Id, matchedInside.Id);

        var outside = (
            await queries.QueryAsync<
                FindLocationsContainingPointQuery,
                IReadOnlyList<LocationResponse>
            >(new FindLocationsContainingPointQuery(34.1000, -112.5000))
        ).Value;
        Assert.Empty(outside);

        var verified = (
            await commands.SendAsync<VerifyLocationAddressCommand, LocationResponse>(
                new VerifyLocationAddressCommand(mainBuilding.Id)
            )
        ).Value;
        Assert.Equal("3120 E CHOLLA ST", verified.Address.Street1);
        _ = Assert.NotNull(verified.StandardizedDateTimeUtc);
        _ = Assert.NotNull(verified.GeocodedDateTimeUtc);
    }

    [Fact]
    public async Task Campuses_LastDeleteGuard_And_Devices_Ip_And_GeoFence_Match_Work()
    {
        var cs = await postgresFixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CoreTestHostFactory.BuildAndMigrateAsync(cs);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        GeoPointDto[] fence =
        [
            new(33.5800, -112.0200),
            new(33.5800, -112.0000),
            new(33.6000, -112.0000),
            new(33.6000, -112.0200),
        ];
        var building = (
            await commands.SendAsync<CreateLocationCommand, LocationResponse>(
                new CreateLocationCommand(Name: "Campus Bldg", GeoFenceRing: fence)
            )
        ).Value;

        var campus1 = (
            await commands.SendAsync<CreateCampusCommand, CampusResponse>(
                new CreateCampusCommand(
                    "Main Campus",
                    ShortCode: "MAIN",
                    TimeZoneId: "America/Phoenix",
                    LocationId: building.Id
                )
            )
        ).Value;

        var cannotDeleteLast = await commands.SendAsync<DeleteCampusCommand, bool>(
            new DeleteCampusCommand(campus1.Id)
        );
        Assert.True(cannotDeleteLast.IsFailure);

        var campus2 = (
            await commands.SendAsync<CreateCampusCommand, CampusResponse>(
                new CreateCampusCommand("West Campus", ShortCode: "WEST")
            )
        ).Value;
        var deletedSecond = await commands.SendAsync<DeleteCampusCommand, bool>(
            new DeleteCampusCommand(campus2.Id)
        );
        Assert.True(deletedSecond.IsSuccess);

        var kioskTypeId = Guid.CreateVersion7();
        var kiosk = (
            await commands.SendAsync<CreateDeviceCommand, DeviceResponse>(
                new CreateDeviceCommand(
                    "Lobby Kiosk 1",
                    kioskTypeId,
                    IpAddress: "10.20.30.40",
                    LocationId: building.Id,
                    PrintFrom: DevicePrintFrom.Server,
                    KioskType: KioskType.IPad,
                    HasCamera: true
                )
            )
        ).Value;

        var matchedByIp = await queries.QueryAsync<MatchDeviceQuery, DeviceResponse>(
            new MatchDeviceQuery(kioskTypeId, IpAddress: "10.20.30.40")
        );
        Assert.True(matchedByIp.IsSuccess);
        Assert.Equal(kiosk.Id, matchedByIp.Value.Id);

        var matchedByGeo = await queries.QueryAsync<MatchDeviceQuery, DeviceResponse>(
            new MatchDeviceQuery(kioskTypeId, Latitude: 33.5900, Longitude: -112.0100)
        );
        Assert.True(matchedByGeo.IsSuccess);
        Assert.Equal(kiosk.Id, matchedByGeo.Value.Id);
    }

    private sealed class FakeAddressProvider : IAddressVerificationProvider
    {
        public string ServiceName => "FakeSmarty";
        public int Order => 0;
        public bool SupportsStandardization => true;
        public bool SupportsGeocoding => true;

        public Task<AddressVerificationOutcome> VerifyAsync(
            LocationAddressDto address,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                new AddressVerificationOutcome(
                    AddressVerificationStatus.Standardized | AddressVerificationStatus.Geocoded,
                    ServiceName,
                    "Verified",
                    address with
                    {
                        Street1 = address.Street1?.ToUpperInvariant(),
                        City = address.City?.ToUpperInvariant(),
                    },
                    new GeoPointDto(33.5898, -112.0132),
                    "85028123456"
                )
            );
        }
    }
}
