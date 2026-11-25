using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class CodeFieldType : FieldTypePluginBase
{
    public override string Key => "code";
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
            0x16
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e016 */
    ;
    public override string Name => "Code Editor";
    public override string Description =>
        "Source code or template snippet (HTML, CSS, SQL, Razor, Markdown).";
    public override FieldEditorKind EditorKind => FieldEditorKind.Code;

    public override IReadOnlyList<FieldQualifierDefinition> Qualifiers =>
        [
            new(
                "editorMode",
                "Language Mode",
                "Syntax language mode (html, css, sql, razor, json, markdown).",
                "html"
            ),
        ];

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        return Result.Success<string?>(value);
    }
}
