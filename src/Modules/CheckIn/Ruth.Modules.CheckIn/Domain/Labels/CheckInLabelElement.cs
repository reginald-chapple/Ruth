using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Domain.Labels;

/// <summary>Validated element inside a <see cref="CheckInLabelLayoutSchema" />.</summary>
public sealed record CheckInLabelElement(
    string ElementKey,
    LabelElementKind Kind,
    int X,
    int Y,
    int Width,
    int Height,
    int FontHeight,
    int FontWidth,
    int MaxLines,
    LabelTextAlignment Alignment,
    bool InvertPrint,
    string ContentTemplate,
    string? ShowOnlyWhenFieldPresent
)
{
    /// <summary>Validates and normalizes a <see cref="CheckInLabelElementDto" />.</summary>
    public static Result<CheckInLabelElement> Create(CheckInLabelElementDto? dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.ElementKey))
        {
            return Result.Failure<CheckInLabelElement>(CheckInErrors.InvalidLabelLayout);
        }

        if (!AreValidCoordinatesAndSizes(dto))
        {
            return Result.Failure<CheckInLabelElement>(CheckInErrors.InvalidLabelLayout);
        }

        var contentResult = CheckInTextPolicy.NormalizeOptional(
            dto.ContentTemplate,
            maxLength: 400
        );
        var condResult = CheckInTextPolicy.NormalizeOptional(
            dto.ShowOnlyWhenFieldPresent,
            maxLength: 80
        );
        var combined = Result.Combine(contentResult, condResult);
        return combined.IsFailure
            ? Result.Failure<CheckInLabelElement>(combined.Errors)
            : Result.Success(
                new CheckInLabelElement(
                    dto.ElementKey.Trim(),
                    dto.Kind,
                    dto.X,
                    dto.Y,
                    dto.Width,
                    dto.Height,
                    Math.Clamp(dto.FontHeight, 12, 240),
                    Math.Clamp(dto.FontWidth, 0, 240),
                    Math.Clamp(dto.MaxLines, 1, 8),
                    dto.Alignment,
                    dto.InvertPrint,
                    contentResult.Value ?? string.Empty,
                    condResult.Value
                )
            );
    }

    /// <summary>Projects back to contract DTO.</summary>
    public CheckInLabelElementDto ToDto()
    {
        return new CheckInLabelElementDto(
            ElementKey,
            Kind,
            X,
            Y,
            Width,
            Height,
            FontHeight,
            FontWidth,
            MaxLines,
            Alignment,
            InvertPrint,
            ContentTemplate,
            ShowOnlyWhenFieldPresent
        );
    }

    private static bool AreValidCoordinatesAndSizes(CheckInLabelElementDto dto)
    {
        return dto.X is >= 0 and <= 2400
            && dto.Y is >= 0 and <= 3200
            && dto.Width is > 0 and <= 2400
            && dto.Height is > 0 and <= 3200;
    }
}
