using System.Text.RegularExpressions;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed partial class ColorFieldType : FieldTypePluginBase
{
    public override string Key => "color";
    public override Guid SystemGuid =>
        new(
            0xa75dfcd5,
            0x277,
            0x44a4,
            0x90,
            0x7e,
            0x7b,
            0x9d,
            0x4c,
            0x25,
            0xe0,
            0x17
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e017 */
    ;
    public override string Name => "Color";
    public override string Description =>
        "Hexadecimal RGB/RGBA color value (for example, #336699).";
    public override FieldEditorKind EditorKind => FieldEditorKind.Color;

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        return HexColorRegex.IsMatch(value)
            ? Result.Success<string?>(value.ToUpperInvariant())
            : ValidationError(
                "Invalid",
                "Color must be a hex value (#RGB, #RRGGBB, or #RRGGBBAA)."
            );
    }

    [GeneratedRegex(
        "^#(?:[0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex HexColorRegex { get; }
}
