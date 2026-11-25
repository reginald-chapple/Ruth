using Ruth.Modules.Core.Domain.FieldTypes;
using Xunit;

namespace Ruth.Modules.Core.Tests.Domain;

public sealed class CoreFieldTypesTests
{
    private static readonly FieldTypeRegistry _registry = new(BuiltInFieldTypes.CreateAll());

    private static readonly IReadOnlyDictionary<string, string> _emptyQualifiers = new Dictionary<
        string,
        string
    >(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void BuiltInRegistry_ContainsAll25FoundationalFieldTypes()
    {
        var all = _registry.GetAll();
        Assert.Equal(25, all.Count);
        Assert.True(_registry.TryGet("text", out _));
        Assert.True(_registry.TryGet("MATRIX", out _));
        Assert.False(_registry.TryGet("unknown-type", out _));
    }

    [Theory]
    [InlineData("text", "  Hello Ruth  ", "Hello Ruth", "Hello Ruth")]
    [InlineData("memo", "Line 1\nLine 2", "Line 1\nLine 2", "Line 1\nLine 2")]
    [InlineData("boolean", "yes", "true", "Yes")]
    [InlineData("boolean", "0", "false", "No")]
    [InlineData("integer", "42", "42", "42")]
    [InlineData("decimal", "12.50", "12.50", "12.50")]
    [InlineData("currency", "19.995", "20.00", "$20.00")]
    [InlineData("date", "2026-11-24", "2026-11-24", "2026-11-24")]
    [InlineData(
        "datetime",
        "2026-11-24T15:30:00Z",
        "2026-11-24T15:30:00.0000000+00:00",
        "2026-11-24T15:30:00.0000000+00:00"
    )]
    [InlineData(
        "daterange",
        "2026-01-01|2026-12-31",
        "2026-01-01|2026-12-31",
        "2026-01-01 to 2026-12-31"
    )]
    [InlineData("time", "09:15:00", "09:15:00", "09:15:00")]
    [InlineData("integerrange", "10|20", "10|20", "10|20")]
    [InlineData("decimalrange", "1.5|9.75", "1.5|9.75", "1.5|9.75")]
    [InlineData(
        "definedvalue",
        "01948000-0000-7000-8000-000000000001",
        "01948000-0000-7000-8000-000000000001",
        "01948000-0000-7000-8000-000000000001"
    )]
    [InlineData(
        "category",
        "01948000-0000-7000-8000-000000000002",
        "01948000-0000-7000-8000-000000000002",
        "01948000-0000-7000-8000-000000000002"
    )]
    [InlineData(
        "keyvaluelist",
        /*lang=json,strict*/
        "{\"campus\":\"Main\",\"role\":\"Pastor\"}",
        /*lang=json,strict*/
        "{\"campus\":\"Main\",\"role\":\"Pastor\"}",
        /*lang=json,strict*/
        "{\"campus\":\"Main\",\"role\":\"Pastor\"}"
    )]
    [InlineData(
        "json", /*lang=json,strict*/
        "{\"a\":1,\"b\":true}", /*lang=json,strict*/
        "{\"a\":1,\"b\":true}", /*lang=json,strict*/
        "{\"a\":1,\"b\":true}"
    )]
    [InlineData("code", "<h1>Hello</h1>", "<h1>Hello</h1>", "<h1>Hello</h1>")]
    [InlineData("color", "#0078d4", "#0078D4", "#0078D4")]
    [InlineData(
        "url",
        "https://ruthrms.org/docs",
        "https://ruthrms.org/docs",
        "https://ruthrms.org/docs"
    )]
    [InlineData("email", "Admin@Grace.Org", "admin@grace.org", "admin@grace.org")]
    [InlineData("phone", "+1 (555) 123-4567", "+15551234567", "+15551234567")]
    [InlineData("rating", "4", "4", "4 / 5")]
    [InlineData(
        "matrix", /*lang=json,strict*/
        "[{\"name\":\"Alpha\"}]", /*lang=json,strict*/
        "[{\"name\":\"Alpha\"}]",
        "1 row"
    )]
    public void BuiltInFieldType_ValidInput_NormalizesAndFormats(
        string key,
        string input,
        string expectedNormalized,
        string expectedFormatted
    )
    {
        Assert.True(_registry.TryGet(key, out var plugin));
        var result = plugin.ValidateAndNormalize(input, _emptyQualifiers, isRequired: true);

        Assert.True(
            result.IsSuccess,
            result.IsFailure ? result.PrimaryError.Description : string.Empty
        );
        Assert.Equal(expectedNormalized, result.Value);
        Assert.Equal(expectedFormatted, plugin.FormatDisplayValue(result.Value, _emptyQualifiers));
    }

    [Theory]
    [InlineData("boolean", "maybe")]
    [InlineData("integer", "not-a-number")]
    [InlineData("decimal", "abc")]
    [InlineData("currency", "$bad")]
    [InlineData("date", "2026-13-40")]
    [InlineData("datetime", "not-a-date")]
    [InlineData("daterange", "2026-12-31|2026-01-01")]
    [InlineData("daterange", "only-one-part")]
    [InlineData("time", "25:99:00")]
    [InlineData("integerrange", "50|10")]
    [InlineData("decimalrange", "10.5|2.1")]
    [InlineData("definedvalue", "not-a-guid")]
    [InlineData("category", "00000000-0000-0000-0000-000000000000")]
    [InlineData("keyvaluelist", "missing-caret")]
    [InlineData("json", "{invalid")]
    [InlineData("color", "blue")]
    [InlineData("url", "javascript:alert(1)")]
    [InlineData("email", "bad-email")]
    [InlineData("phone", "abc")]
    [InlineData("rating", "99")]
    [InlineData(
        "matrix", /*lang=json,strict*/
        "{\"not\":\"array\"}"
    )]
    public void BuiltInFieldType_InvalidInput_ReturnsValidationFailure(string key, string input)
    {
        Assert.True(_registry.TryGet(key, out var plugin));
        var result = plugin.ValidateAndNormalize(input, _emptyQualifiers, isRequired: true);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void SelectFieldTypes_EnforceAllowedOptionsAndDeduplicate()
    {
        var qualifiers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["values"] = "small^Small,medium^Medium,large^Large",
        };

        Assert.True(_registry.TryGet("singleselect", out var single));
        var singleOk = single.ValidateAndNormalize("medium", qualifiers, isRequired: true);
        Assert.True(singleOk.IsSuccess);
        Assert.Equal("medium", singleOk.Value);
        Assert.Equal("Medium", single.FormatDisplayValue(singleOk.Value, qualifiers));
        Assert.True(single.ValidateAndNormalize("xlarge", qualifiers, isRequired: true).IsFailure);

        Assert.True(_registry.TryGet("multiselect", out var multi));
        var multiOk = multi.ValidateAndNormalize(
            "small, large, small",
            qualifiers,
            isRequired: true
        );
        Assert.True(multiOk.IsSuccess);
        Assert.Equal("small,large", multiOk.Value);
        Assert.Equal("Small, Large", multi.FormatDisplayValue(multiOk.Value, qualifiers));
        Assert.True(
            multi.ValidateAndNormalize("small,nope", qualifiers, isRequired: true).IsFailure
        );
    }

    [Fact]
    public void OptionalVsRequiredAndQualifiers_AreEnforcedAcrossNumericAndTextTypes()
    {
        Assert.True(_registry.TryGet("integer", out var integer));
        var optionalEmpty = integer.ValidateAndNormalize(
            "   ",
            _emptyQualifiers,
            isRequired: false
        );
        Assert.True(optionalEmpty.IsSuccess);
        Assert.Null(optionalEmpty.Value);
        Assert.True(
            integer
                .ValidateAndNormalize(rawValue: null, _emptyQualifiers, isRequired: true)
                .IsFailure
        );

        var bounds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["minValue"] = "1",
            ["maxValue"] = "10",
        };
        Assert.True(integer.ValidateAndNormalize("0", bounds, isRequired: true).IsFailure);
        Assert.True(integer.ValidateAndNormalize("11", bounds, isRequired: true).IsFailure);
        Assert.True(integer.ValidateAndNormalize("5", bounds, isRequired: true).IsSuccess);

        Assert.True(_registry.TryGet("text", out var text));
        var maxLen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["maxLength"] = "4",
        };
        Assert.True(text.ValidateAndNormalize("12345", maxLen, isRequired: true).IsFailure);
        Assert.True(text.ValidateAndNormalize("1234", maxLen, isRequired: true).IsSuccess);
    }
}
