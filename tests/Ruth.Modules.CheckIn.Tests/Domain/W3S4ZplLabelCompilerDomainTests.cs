using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Labels;
using Xunit;

namespace Ruth.Modules.CheckIn.Tests.Domain;

public sealed class W3S4ZplLabelCompilerDomainTests
{
    [Fact]
    public void CheckInLabel_ValidatesDimensionsKeysElements_AndProtectsSystemLabelsFromDeletion()
    {
        var defaultLayout = CheckInLabelLayoutSchema.CreateDefaultParticipantBadgeDto();

        var invalidKey = CheckInLabel.Create(
            new CheckInLabelSpec(
                "Bad Key Label",
                "INVALID KEY!",
                CheckInLabelKind.ParticipantBadge,
                defaultLayout,
                Description: null,
                ConfigurationId: null,
                AreaId: null,
                CustomZplOverride: null,
                PrintOrder: 0,
                Copies: 1,
                IsActive: true
            )
        );
        Assert.True(invalidKey.IsFailure);
        Assert.Equal(CheckInErrors.LabelKeyInvalid.Code, invalidKey.PrimaryError.Code);

        var badCopies = CheckInLabel.Create(
            new CheckInLabelSpec(
                "Too Many Copies",
                "too-many-copies",
                CheckInLabelKind.ParticipantBadge,
                defaultLayout,
                Description: null,
                ConfigurationId: null,
                AreaId: null,
                CustomZplOverride: null,
                PrintOrder: 0,
                Copies: 9,
                IsActive: true
            )
        );
        Assert.True(badCopies.IsFailure);

        var invalidSize = CheckInLabelLayoutSchema.Create(
            defaultLayout with
            {
                WidthInches = 0.25m,
            }
        );
        Assert.True(invalidSize.IsFailure);

        var dupElementKey = CheckInLabelLayoutSchema.Create(
            new CheckInLabelLayoutDto(
                4.0m,
                2.0m,
                LabelDpi.Dpi203,
                [defaultLayout.Elements[0], defaultLayout.Elements[0]]
            )
        );
        Assert.True(dupElementKey.IsFailure);

        var systemLabel = CheckInLabel.Create(
            new CheckInLabelSpec(
                "System Badge",
                "system-badge",
                CheckInLabelKind.ParticipantBadge,
                defaultLayout,
                Description: "Protected",
                ConfigurationId: null,
                AreaId: null,
                CustomZplOverride: null,
                PrintOrder: 0,
                Copies: 1,
                IsActive: true,
                IsSystem: true
            )
        );
        Assert.True(systemLabel.IsSuccess);
        Assert.True(systemLabel.Value.EnsureDeletable().IsFailure);
    }

    [Fact]
    public void ZplLabelCompiler_CompilesInvertedBlocks_ConditionalAlerts_Barcodes_QrCodes_AndPreventsZplCommandInjection()
    {
        var dto = new CheckInLabelLayoutDto(
            4.0m,
            2.0m,
            LabelDpi.Dpi203,
            [
                new CheckInLabelElementDto(
                    "border-box",
                    LabelElementKind.BoxOrRule,
                    10,
                    10,
                    792,
                    386
                ),
                new CheckInLabelElementDto(
                    "name",
                    LabelElementKind.TextField,
                    24,
                    24,
                    500,
                    60,
                    FontHeight: 54,
                    ContentTemplate: "{{ Participant.NickName }} {{ Participant.LastName }}"
                ),
                new CheckInLabelElementDto(
                    "code",
                    LabelElementKind.SecurityCodeBlock,
                    560,
                    24,
                    210,
                    80,
                    FontHeight: 48,
                    Alignment: LabelTextAlignment.Center,
                    InvertPrint: true,
                    ContentTemplate: "{{ Session.SecurityPickupCode }}",
                    ShowOnlyWhenFieldPresent: "Session.SecurityPickupCode"
                ),
                new CheckInLabelElementDto(
                    "alert",
                    LabelElementKind.AlertBanner,
                    24,
                    120,
                    760,
                    48,
                    FontHeight: 28,
                    InvertPrint: true,
                    ContentTemplate: "ALERT: {{ Alerts.Combined }}",
                    ShowOnlyWhenFieldPresent: "Alerts.Combined"
                ),
                new CheckInLabelElementDto(
                    "icons",
                    LabelElementKind.BadgeIcon,
                    24,
                    180,
                    400,
                    32,
                    Alignment: LabelTextAlignment.Right,
                    ContentTemplate: "{{ Badge.Icons }}",
                    ShowOnlyWhenFieldPresent: "Badge.Icons"
                ),
                new CheckInLabelElementDto(
                    "barcode",
                    LabelElementKind.BarcodeCode128,
                    24,
                    230,
                    360,
                    80,
                    ContentTemplate: "{{ Barcode.Value }}"
                ),
                new CheckInLabelElementDto(
                    "qr",
                    LabelElementKind.QrCode,
                    420,
                    230,
                    120,
                    120,
                    ContentTemplate: "{{ Barcode.Value }}"
                ),
            ]
        );
        var layout = CheckInLabelLayoutSchema.Create(dto).Value;

        // Attempt ZPL command injection inside NickName (^XZ^XA~JA\test_raw).
        var maliciousContext = new CheckInLabelMergeContextDto(
            ParticipantFullName: "Evil^XZ^XA~JA\\User_1",
            ParticipantNickName: "Evil^XZ~JA\\User_1",
            ParticipantLastName: "Rivera",
            SecurityPickupCode: "BC47",
            MedicalAllergyAlert: "Peanuts",
            ComplianceAlert: "Check ID",
            AccommodationNote: "Quiet Area",
            MembershipTier: "Gold",
            BadgeIconCodes: ["cpr", "stem"],
            BarcodeValue: "CHK-BC47"
        );

        var preview = ZplLabelCompiler.Compile("Safety Badge", layout, maliciousContext, copies: 2);
        Assert.Equal(812, preview.WidthDots);
        Assert.Equal(406, preview.HeightDots);
        Assert.Contains("^XA", preview.CompiledZpl, StringComparison.Ordinal);
        Assert.Contains("^PW812", preview.CompiledZpl, StringComparison.Ordinal);
        Assert.Contains("^LL406", preview.CompiledZpl, StringComparison.Ordinal);
        Assert.Contains("^FR", preview.CompiledZpl, StringComparison.Ordinal);
        Assert.Contains("^BCN", preview.CompiledZpl, StringComparison.Ordinal);
        Assert.Contains("^BQN", preview.CompiledZpl, StringComparison.Ordinal);
        Assert.Contains("^PQ2,0,1,Y", preview.CompiledZpl, StringComparison.Ordinal);

        // Verify ^, ~, \, and _ in user input were hex-escaped and cannot inject ^XZ!
        Assert.Contains("Evil_5EXZ_7EJA_5CUser_5F1", preview.CompiledZpl, StringComparison.Ordinal);
        Assert.DoesNotContain("Evil^XZ", preview.CompiledZpl, StringComparison.Ordinal);
        Assert.Equal(7, preview.VisualElements.Count);

        // When optional conditional fields (Alerts.Combined, SecurityPickupCode, Badge.Icons) are absent, they are omitted!
        var cleanContext = new CheckInLabelMergeContextDto(
            ParticipantNickName: "Sam",
            ParticipantLastName: "Lee",
            SecurityPickupCode: null,
            MedicalAllergyAlert: null,
            ComplianceAlert: null,
            AccommodationNote: null,
            MembershipTier: null,
            BadgeIconCodes: null,
            BarcodeValue: "SAM-01"
        );
        var minimalPreview = ZplLabelCompiler.Compile("Minimal Badge", layout, cleanContext);
        Assert.DoesNotContain("ALERT:", minimalPreview.CompiledZpl, StringComparison.Ordinal);
        Assert.Equal(4, minimalPreview.VisualElements.Count);

        // Custom ZPL override also sanitizes interpolated tokens.
        var customZpl = ZplLabelCompiler.Compile(
            "Custom Override",
            layout,
            maliciousContext,
            customZplOverride: "^XA^FO20,20^FH_^FD{{ Participant.NickName }}^FS^XZ"
        );
        Assert.Contains(
            "Evil_5EXZ_7EJA_5CUser_5F1",
            customZpl.CompiledZpl,
            StringComparison.Ordinal
        );
    }
}
