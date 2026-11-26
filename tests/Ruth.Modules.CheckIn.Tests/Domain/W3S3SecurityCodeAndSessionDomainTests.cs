using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Sessions;
using Xunit;

namespace Ruth.Modules.CheckIn.Tests.Domain;

public sealed class W3S3SecurityCodeAndSessionDomainTests
{
    [Fact]
    public void SecurityPickupCodeGenerator_EnforcesLengths_AvoidsAmbiguousChars_AndFiltersProfanityAndLeetspeak()
    {
        Assert.True(SecurityPickupCodeGenerator.Generate(-1, 2).IsFailure);
        Assert.True(SecurityPickupCodeGenerator.Generate(5, 5).IsFailure);
        Assert.True(SecurityPickupCodeGenerator.Generate(0, 0).IsFailure);

        Assert.False(SecurityPickupCodeGenerator.IsSafeCode("FCK9"));
        Assert.False(SecurityPickupCodeGenerator.IsSafeCode("SH17"));
        Assert.False(SecurityPickupCodeGenerator.IsSafeCode("F4G2"));
        Assert.False(SecurityPickupCodeGenerator.IsSafeCode("X666"));
        Assert.False(SecurityPickupCodeGenerator.IsSafeCode("VIP1", ["VIP"]));
        Assert.True(SecurityPickupCodeGenerator.IsSafeCode("BC47"));

        var issued = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA00" };
        var calls = 0;
        int[] scripted = [0, 0, 0, 0, 1, 2, 4, 7];
        var generated = SecurityPickupCodeGenerator.Generate(
            alphaLength: 2,
            numericLength: 2,
            alreadyIssuedToday: issued,
            randomIndexSelector: _ => scripted[calls++ % scripted.Length]
        );

        Assert.True(generated.IsSuccess);
        Assert.Equal("BC47", generated.Value);
        Assert.DoesNotContain('I', generated.Value);
        Assert.DoesNotContain('O', generated.Value);

        var exhausted = SecurityPickupCodeGenerator.Generate(
            alphaLength: 1,
            numericLength: 0,
            alreadyIssuedToday: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" },
            randomIndexSelector: static _ => 0,
            maxAttempts: 5
        );
        Assert.True(exhausted.IsFailure);
        Assert.Equal(CheckInErrors.SecurityCodePoolExhausted.Code, exhausted.PrimaryError.Code);

        var express = SecurityPickupCodeGenerator.GenerateMobileExpressToken();
        Assert.StartsWith("CHK-", express, StringComparison.Ordinal);
        Assert.Equal(10, express.Length);
    }

    [Fact]
    public void AttendanceCheckInSession_EnforcesEntryInvariants_CheckOutCodeVerification_RoomMove_AndPersonMerge()
    {
        var now = new DateTimeOffset(2026, 11, 25, 16, 0, 0, TimeSpan.Zero);
        var serviceDate = new DateOnly(2026, 11, 25);
        var configId = CheckInConfigurationId.Create();
        var areaId = CheckInAreaId.Create();
        var personId = Guid.CreateVersion7();
        var guardianId = Guid.CreateVersion7();
        var room1 = Guid.CreateVersion7();
        var room2 = Guid.CreateVersion7();
        var scheduleId = Guid.CreateVersion7();

        var header = new CheckInSessionCreationSpec(
            configId,
            "Youth Check-In",
            BranchId: null,
            KioskDeviceId: null,
            HouseholdId: null,
            CheckedInByPersonId: guardianId,
            CheckInSessionChannel.Kiosk,
            CheckInSearchMode.PhoneAndName,
            SearchTermSummary: "8241",
            serviceDate,
            SecurityPickupCode: "BC47",
            MobileExpressToken: null,
            ClientIpAddress: "127.0.0.1",
            now
        );

        Assert.True(AttendanceCheckInSession.Create(header, []).IsFailure);

        var spec = new CheckInEntrySpec(
            areaId,
            "Kids Studio",
            LinkedGroupId: null,
            personId,
            "Maya Rivera",
            room1,
            "Room 101",
            scheduleId,
            serviceDate,
            SecurityPickupCode: "BC47",
            PrintBadgeRequested: true,
            IsCapacityOverride: false,
            IsEligibilityOverride: false,
            OverrideByPersonId: null,
            OverrideReason: null,
            AttendanceNote: "Dairy allergy",
            now
        );

        Assert.True(AttendanceCheckInSession.Create(header, [spec, spec]).IsFailure);

        var sessionResult = AttendanceCheckInSession.Create(header, [spec]);
        Assert.True(sessionResult.IsSuccess);
        var session = sessionResult.Value;
        var entry = Assert.Single(session.Entries);
        _ = Assert.Single(session.IntegrationEvents);

        var badCode = session.CheckOutEntry(
            entry.Id,
            now.AddHours(1),
            guardianId,
            providedPickupCode: "ZZ99",
            isStaffOverride: false,
            overrideReason: null
        );
        Assert.True(badCode.IsFailure);
        Assert.Equal(CheckInErrors.InvalidSecurityPickupCode.Code, badCode.PrimaryError.Code);

        var moved = session.ReassignEntryRoom(
            entry.Id,
            areaId,
            "Kids Studio",
            room2,
            "Room 102",
            guardianId,
            reason: "Balancing rooms",
            isCapacityOverride: true
        );
        Assert.True(moved.IsSuccess);
        Assert.Equal(room2, entry.LocationId);
        Assert.True(entry.IsCapacityOverride);

        var checkedOut = session.CheckOutEntry(
            entry.Id,
            now.AddHours(1),
            guardianId,
            providedPickupCode: "bc47",
            isStaffOverride: false,
            overrideReason: null
        );
        Assert.True(checkedOut.IsSuccess);
        Assert.Equal(CheckInEntryStatus.CheckedOut, entry.Status);
        Assert.Equal(2, session.IntegrationEvents.Count);

        Assert.True(
            session
                .CheckOutEntry(
                    entry.Id,
                    now.AddHours(2),
                    guardianId,
                    providedPickupCode: "BC47",
                    isStaffOverride: false,
                    overrideReason: null
                )
                .IsFailure
        );

        var survivingPersonId = Guid.CreateVersion7();
        session.RepointPerson(personId, survivingPersonId);
        session.RepointPerson(guardianId, survivingPersonId);
        Assert.Equal(survivingPersonId, entry.PersonId);
        Assert.Equal(survivingPersonId, entry.CheckedOutByPersonId);
        Assert.Equal(survivingPersonId, entry.OverrideByPersonId);
        Assert.Equal(survivingPersonId, session.CheckedInByPersonId);

        Assert.True(
            session
                .ReassignEntryRoom(
                    entry.Id,
                    areaId,
                    "Kids Studio",
                    room1,
                    "Room 101",
                    guardianId,
                    reason: "Too late",
                    isCapacityOverride: false
                )
                .IsFailure
        );
    }
}
