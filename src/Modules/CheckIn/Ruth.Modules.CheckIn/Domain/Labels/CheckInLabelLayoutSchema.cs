using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Domain.Labels;

/// <summary>Validated thermal label layout value object supporting 203/300 DPI Zebra ZPL II compilation.</summary>
public sealed class CheckInLabelLayoutSchema
{
    private CheckInLabelLayoutSchema(
        decimal widthInches,
        decimal heightInches,
        LabelDpi dpi,
        IReadOnlyList<CheckInLabelElement> elements
    )
    {
        WidthInches = widthInches;
        HeightInches = heightInches;
        Dpi = dpi;
        Elements = elements;
    }

    /// <summary>Gets the label width in inches.</summary>
    public decimal WidthInches { get; }

    /// <summary>Gets the label height in inches.</summary>
    public decimal HeightInches { get; }

    /// <summary>Gets the printhead resolution in dots per inch.</summary>
    public LabelDpi Dpi { get; }

    /// <summary>Gets the validated positioned elements.</summary>
    public IReadOnlyList<CheckInLabelElement> Elements { get; }

    /// <summary>Gets the computed label width in printhead dots.</summary>
    public int WidthDots =>
        (int)Math.Round(WidthInches * (int)Dpi, 0, MidpointRounding.AwayFromZero);

    /// <summary>Gets the computed label height in printhead dots.</summary>
    public int HeightDots =>
        (int)Math.Round(HeightInches * (int)Dpi, 0, MidpointRounding.AwayFromZero);

    /// <summary>Validates and creates a <see cref="CheckInLabelLayoutSchema" />.</summary>
    public static Result<CheckInLabelLayoutSchema> Create(
        CheckInLabelLayoutDto? dto,
        bool allowEmptyElements = false
    )
    {
        if (dto is null || !AreValidDimensions(dto.WidthInches, dto.HeightInches, dto.Dpi))
        {
            return Result.Failure<CheckInLabelLayoutSchema>(CheckInErrors.InvalidLabelLayout);
        }

        var rawElements = dto.Elements ?? [];
        if ((!allowEmptyElements && rawElements.Count == 0) || rawElements.Count > 50)
        {
            return Result.Failure<CheckInLabelLayoutSchema>(CheckInErrors.InvalidLabelLayout);
        }

        var validatedResult = ValidateElements(rawElements);
        return validatedResult.IsFailure
            ? Result.Failure<CheckInLabelLayoutSchema>(validatedResult.Errors)
            : Result.Success(
                new CheckInLabelLayoutSchema(
                    Math.Round(dto.WidthInches, 2, MidpointRounding.AwayFromZero),
                    Math.Round(dto.HeightInches, 2, MidpointRounding.AwayFromZero),
                    dto.Dpi,
                    validatedResult.Value
                )
            );
    }

    /// <summary>Converts back to contract DTO.</summary>
    public CheckInLabelLayoutDto ToDto()
    {
        return new CheckInLabelLayoutDto(
            WidthInches,
            HeightInches,
            Dpi,
            [.. Elements.Select(static e => e.ToDto())]
        );
    }

    /// <summary>Creates the clean-room default 4" x 2" (203 DPI) Participant Safety Badge layout.</summary>
    public static CheckInLabelLayoutDto CreateDefaultParticipantBadgeDto()
    {
        return new CheckInLabelLayoutDto(
            4.0m,
            2.0m,
            LabelDpi.Dpi203,
            [.. BuildParticipantTopElements(), .. BuildParticipantBottomElements()]
        );
    }

    /// <summary>Creates the clean-room default 4" x 2" (203 DPI) Guardian Security Pickup Receipt layout.</summary>
    public static CheckInLabelLayoutDto CreateDefaultGuardianReceiptDto()
    {
        return new CheckInLabelLayoutDto(
            4.0m,
            2.0m,
            LabelDpi.Dpi203,
            [
                TextEl("receipt-header", 24, 22, 520, 36, 30, "GUARDIAN PICKUP RECEIPT"),
                CodeEl(
                    "pickup-code",
                    24,
                    70,
                    340,
                    110,
                    74,
                    "{{ Session.SecurityPickupCode }}",
                    condition: null
                ),
                new CheckInLabelElementDto(
                    "pickup-barcode",
                    LabelElementKind.BarcodeCode128,
                    400,
                    78,
                    380,
                    90,
                    FontHeight: 24,
                    ContentTemplate: "{{ Barcode.Value }}"
                ),
                new CheckInLabelElementDto(
                    "guardian-summary",
                    LabelElementKind.TextField,
                    24,
                    205,
                    764,
                    110,
                    FontHeight: 26,
                    MaxLines: 3,
                    ContentTemplate: "{{ Guardian.ReceiptSummary }}"
                ),
                TextEl(
                    "receipt-date",
                    24,
                    340,
                    764,
                    28,
                    22,
                    "Present this receipt at check-out | {{ Session.ServiceDate }}"
                ),
            ]
        );
    }

    private static CheckInLabelElementDto[] BuildParticipantTopElements()
    {
        return
        [
            TextEl("nick-name", 24, 24, 540, 72, 68, "{{ Participant.NickName }}"),
            TextEl("last-name", 24, 102, 540, 40, 34, "{{ Participant.LastName }}"),
            CodeEl(
                "pickup-code",
                580,
                20,
                208,
                96,
                56,
                "{{ Session.SecurityPickupCode }}",
                "Session.SecurityPickupCode"
            ),
            TextEl(
                "room-zone",
                24,
                160,
                540,
                36,
                28,
                "{{ Entry.AreaName }} - {{ Entry.LocationName }}"
            ),
        ];
    }

    private static CheckInLabelElementDto[] BuildParticipantBottomElements()
    {
        return
        [
            new CheckInLabelElementDto(
                "tier-icons",
                LabelElementKind.BadgeIcon,
                580,
                132,
                208,
                36,
                FontHeight: 24,
                Alignment: LabelTextAlignment.Center,
                ContentTemplate: "{{ Badge.Icons }}",
                ShowOnlyWhenFieldPresent: "Badge.Icons"
            ),
            new CheckInLabelElementDto(
                "alert-banner",
                LabelElementKind.AlertBanner,
                24,
                220,
                764,
                56,
                FontHeight: 28,
                InvertPrint: true,
                ContentTemplate: "ALERT: {{ Alerts.Combined }}",
                ShowOnlyWhenFieldPresent: "Alerts.Combined"
            ),
            TextEl(
                "footer-time",
                24,
                340,
                764,
                28,
                22,
                "{{ Session.ServiceDate }} {{ Session.CheckedInTime }} | {{ Entry.ScheduleName }}"
            ),
        ];
    }

    private static CheckInLabelElementDto TextEl(
        string key,
        int x,
        int y,
        int width,
        int height,
        int font,
        string content
    )
    {
        return new CheckInLabelElementDto(
            key,
            LabelElementKind.TextField,
            x,
            y,
            width,
            height,
            FontHeight: font,
            ContentTemplate: content
        );
    }

    private static CheckInLabelElementDto CodeEl(
        string key,
        int x,
        int y,
        int width,
        int height,
        int font,
        string content,
        string? condition
    )
    {
        return new CheckInLabelElementDto(
            key,
            LabelElementKind.SecurityCodeBlock,
            x,
            y,
            width,
            height,
            FontHeight: font,
            Alignment: LabelTextAlignment.Center,
            InvertPrint: true,
            ContentTemplate: content,
            ShowOnlyWhenFieldPresent: condition
        );
    }

    private static bool AreValidDimensions(decimal width, decimal height, LabelDpi dpi)
    {
        return width is >= 1.0m and <= 6.0m
            && height is >= 0.5m and <= 8.0m
            && dpi is LabelDpi.Dpi203 or LabelDpi.Dpi300;
    }

    private static Result<List<CheckInLabelElement>> ValidateElements(
        IReadOnlyList<CheckInLabelElementDto> rawElements
    )
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<CheckInLabelElement>(rawElements.Count);
        foreach (var raw in rawElements)
        {
            var created = CheckInLabelElement.Create(raw);
            if (created.IsFailure)
            {
                return Result.Failure<List<CheckInLabelElement>>(created.Errors);
            }

            if (!keys.Add(created.Value.ElementKey))
            {
                return Result.Failure<List<CheckInLabelElement>>(CheckInErrors.InvalidLabelLayout);
            }

            list.Add(created.Value);
        }

        return Result.Success(list);
    }
}
