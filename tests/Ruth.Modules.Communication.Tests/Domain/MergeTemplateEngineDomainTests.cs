using System.Reflection;
using System.Text.Json;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Domain.Templates;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Domain;

public sealed class MergeTemplateEngineDomainTests
{
    private sealed class InMemorySnippetStore(
        IReadOnlyDictionary<string, TemplateSnippetDefinition>? snippets = null
    ) : ITemplateSnippetStore
    {
        private readonly IReadOnlyDictionary<string, TemplateSnippetDefinition> _snippets = snippets
            ?? new Dictionary<string, TemplateSnippetDefinition>(StringComparer.OrdinalIgnoreCase);

        public Task<TemplateSnippetDefinition?> FindActiveByKeyAsync(
            string key,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(_snippets.GetValueOrDefault(key));
        }
    }

    private sealed record SamplePersonDto(
        string NickName,
        string LastName,
        int Age,
        decimal GiftAmount,
        DateTimeOffset JoinedOn,
        DateOnly BirthDate,
        bool IsMember,
        SampleAddressDto Address
    );

    private sealed record SampleAddressDto(string City, string State);

    private sealed record UnsafeDtoWrapper(
        string SafeName,
        Assembly UnsafeAssembly,
        Action UnsafeCallback
    );

    [Fact]
    public async Task Render_Interpolates_Nested_Dtos_Dictionaries_And_HtmlEncodes_Xss()
    {
        var engine = new SafeMergeTemplateEngine(new InMemorySnippetStore());
        var person = new SamplePersonDto(
            "<script>alert('xss')</script>",
            "Smith",
            34,
            1250.5m,
            new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero),
            new DateOnly(1992, 5, 10),
            IsMember: true,
            new SampleAddressDto("Denver", "CO")
        );
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Person"] = person,
            ["Tags"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Campus"] = "North & East",
            },
        };

        var htmlResult = await engine.RenderAsync(
            "<p>Hello {{ person.nickname }} from {{ Person.Address.City }}, {{ Tags.Campus }}!</p>",
            context,
            MergeRenderOptions.DefaultHtml
        );

        Assert.True(htmlResult.IsSuccess);
        Assert.Equal(
            "<p>Hello &lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt; from Denver, North &amp; East!</p>",
            htmlResult.Value.RenderedContent
        );
        Assert.Empty(htmlResult.Value.MissingTokens);

        var plainResult = await engine.RenderAsync(
            "SMS: {{ Person.NickName }} & {{ Tags.Campus }}",
            context,
            MergeRenderOptions.DefaultPlainText
        );
        Assert.True(plainResult.IsSuccess);
        Assert.Equal(
            "SMS: <script>alert('xss')</script> & North & East",
            plainResult.Value.RenderedContent
        );
    }

    [Fact]
    public async Task Render_Applies_All_Safe_Formatters_And_Chains()
    {
        var engine = new SafeMergeTemplateEngine(new InMemorySnippetStore());
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Name"] = "  jordan smith  ",
            ["Missing"] = "",
            ["Amount"] = 42.5m,
            ["Count"] = 12345,
            ["DoubleVal"] = 12.34d,
            ["FloatVal"] = 5.5f,
            ["LongVal"] = 1000L,
            ["AmountStr"] = "99.95",
            ["NotANumber"] = "abc",
            ["EventDate"] = new DateTimeOffset(2026, 11, 24, 15, 30, 0, TimeSpan.Zero),
            ["BirthDate"] = new DateOnly(2000, 1, 15),
            ["LegacyDate"] = new DateTime(2026, 12, 25, 0, 0, 0, DateTimeKind.Utc),
            ["IsoDate"] = "2026-07-04",
            ["BadDate"] = "not-a-date",
            ["Phone10"] = "303-555-1212",
            ["Phone11"] = "+1 (303) 555-9876",
            ["ShortPhone"] = "555-12",
            ["Query"] = "hello world & friends",
            ["Multiline"] = "Line 1\r\n<b>Line 2</b>\nLine 3",
            ["LongText"] = "This is a long sentence for truncation",
        };

        var template = string.Join(
            "|",
            [
                "{{ Name | trim | title }}",
                "{{ Name | trim | upper }}",
                "{{ Name | trim | lower }}",
                "{{ Missing | default:'Friend' }}",
                "{{ LongText | truncate:10 }}",
                "{{ LongText | truncate:bad }}",
                "{{ Amount | currency }}",
                "{{ Amount | currency:'usd' }}",
                "{{ NotANumber | currency }}",
                "{{ Count | number:'N0' }}",
                "{{ DoubleVal | number:'N1' }}",
                "{{ FloatVal | number:'N1' }}",
                "{{ LongVal | number:'N0' }}",
                "{{ AmountStr | number:'N2' }}",
                "{{ NotANumber | number }}",
                "{{ EventDate | date:'yyyy-MM-dd' }}",
                "{{ BirthDate | date:'yyyy-MM-dd' }}",
                "{{ LegacyDate | date:'yyyy-MM-dd' }}",
                "{{ IsoDate | date:'yyyy-MM-dd' }}",
                "{{ BadDate | date:'yyyy-MM-dd' }}",
                "{{ Phone10 | phone }}",
                "{{ Phone11 | phone }}",
                "{{ ShortPhone | phone }}",
                "{{ Query | url_encode }}",
                "{{ Multiline | newlines_to_br }}",
            ]
        );

        var result = await engine.RenderAsync(
            template,
            context,
            new MergeRenderOptions(TemplateOutputMode.Html, CultureName: "en-US")
        );

        Assert.True(result.IsSuccess);
        var parts = result.Value.RenderedContent.Split('|');
        Assert.Equal("Jordan Smith", parts[0]);
        Assert.Equal("JORDAN SMITH", parts[1]);
        Assert.Equal("jordan smith", parts[2]);
        Assert.Equal("Friend", parts[3]);
        Assert.Equal("This is a …", parts[4]);
        Assert.Equal("This is a long sentence for truncation", parts[5]);
        Assert.Equal("$42.50", parts[6]);
        Assert.Equal("USD 42.50", parts[7]);
        Assert.Equal("abc", parts[8]);
        Assert.Equal("12,345", parts[9]);
        Assert.Equal("12.3", parts[10]);
        Assert.Equal("5.5", parts[11]);
        Assert.Equal("1,000", parts[12]);
        Assert.Equal("99.95", parts[13]);
        Assert.Equal("abc", parts[14]);
        Assert.Equal("2026-11-24", parts[15]);
        Assert.Equal("2000-01-15", parts[16]);
        Assert.Equal("2026-12-25", parts[17]);
        Assert.Equal("2026-07-04", parts[18]);
        Assert.Equal("not-a-date", parts[19]);
        Assert.Equal("(303) 555-1212", parts[20]);
        Assert.Equal("(303) 555-9876", parts[21]);
        Assert.Equal("555-12", parts[22]);
        Assert.Equal("hello%20world%20%26%20friends", parts[23]);
        Assert.Equal("Line 1<br />&lt;b&gt;Line 2&lt;/b&gt;<br />Line 3", parts[24]);

        var plainNewlines = await engine.RenderAsync(
            "{{ Multiline | newlines_to_br }}",
            context,
            MergeRenderOptions.DefaultPlainText
        );
        Assert.True(plainNewlines.IsSuccess);
        Assert.Equal("Line 1\r\n<b>Line 2</b>\nLine 3", plainNewlines.Value.RenderedContent);
    }

    [Fact]
    public async Task Render_Evaluates_Conditionals_Truthiness_And_Blocks_Unsafe_Reflection_Types()
    {
        var engine = new SafeMergeTemplateEngine(new InMemorySnippetStore());
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["FlagTrue"] = true,
            ["FlagFalse"] = false,
            ["NonZero"] = 5,
            ["Zero"] = 0,
            ["LongNonZero"] = 42L,
            ["LongZero"] = 0L,
            ["DecNonZero"] = 1.5m,
            ["DecZero"] = 0m,
            ["DblNonZero"] = 2.5d,
            ["DblZero"] = 0d,
            ["TextNonEmpty"] = "hello",
            ["TextBlank"] = "   ",
            ["ObjTruthy"] = new SampleAddressDto("Austin", "TX"),
            ["NullVal"] = null,
            ["Items"] = new List<string> { "a" },
            ["EmptyItems"] = new List<string>(),
            ["CaseSensitiveDict"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["MixedKey"] = "Matched",
            },
            ["CaseSensitiveStrings"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ColorCode"] = "Blue",
            },
            ["Wrapper"] = new UnsafeDtoWrapper(
                "Safe",
                typeof(MergeTemplateEngineDomainTests).Assembly,
                static () => { }
            ),
        };

        var result = await engine.RenderAsync(
            "{{#if FlagTrue}}T{{#else}}F{{/if}}-{{#if FlagFalse}}T{{#else}}F{{/if}}-{{#if NonZero}}N{{/if}}-{{#if Zero}}Z{{#else}}0{{/if}}-{{#if LongNonZero}}L{{/if}}{{#if LongZero}}x{{/if}}-{{#if DecNonZero}}D{{/if}}{{#if DecZero}}x{{/if}}-{{#if DblNonZero}}F{{/if}}{{#if DblZero}}x{{/if}}-{{#if TextNonEmpty}}S{{/if}}{{#if TextBlank}}x{{/if}}-{{#if ObjTruthy}}O{{/if}}{{#if NullVal}}x{{/if}}-{{#if Items}}I{{/if}}-{{#if EmptyItems}}E{{#else}}X{{/if}}-{{ casesensitivedict.mixedkey }}-{{ casesensitivedict.missing }}-{{ casesensitivestrings.colorcode }}-{{ casesensitivestrings.missing }}-{{ Wrapper.SafeName }}-{{ Wrapper.UnsafeAssembly.FullName }}-{{ Wrapper.UnsafeCallback.Method }}",
            context
        );

        Assert.True(result.IsSuccess);
        Assert.Equal("T-F-N-0-L-D-F-S-O-I-X-Matched--Blue--Safe--", result.Value.RenderedContent);
        Assert.Contains(
            "Wrapper.UnsafeAssembly.FullName",
            result.Value.MissingTokens,
            StringComparer.Ordinal
        );
        Assert.Contains(
            "Wrapper.UnsafeCallback.Method",
            result.Value.MissingTokens,
            StringComparer.Ordinal
        );
    }

    [Fact]
    public async Task Render_Expands_Snippets_Overrides_Defaults_And_Stops_Cycles_And_Deep_Recursion()
    {
        var snippets = new Dictionary<string, TemplateSnippetDefinition>(
            StringComparer.OrdinalIgnoreCase
        )
        {
            ["cta_button"] = new(
                "cta_button",
                "<a href=\"{{ url }}\">{{ label }}</a>",
                TemplateOutputMode.Html,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["label"] = "Learn More",
                    ["url"] = "https://default.example",
                }
            ),
            ["wrapper"] = new(
                "wrapper",
                "<div>{{ snippet:cta_button label=customLabel url=\"https://give.example\" }}</div>",
                TemplateOutputMode.Html,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["customLabel"] = "Give Today",
                }
            ),
            ["cycle_a"] = new(
                "cycle_a",
                "{{ snippet:cycle_b }}",
                TemplateOutputMode.Html,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            ),
            ["cycle_b"] = new(
                "cycle_b",
                "{{ snippet:cycle_a }}",
                TemplateOutputMode.Html,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            ),
        };

        var engine = new SafeMergeTemplateEngine(new InMemorySnippetStore(snippets));
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Links"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Give"] = "https://church.example/give",
            },
        };

        var ok = await engine.RenderAsync(
            "{{ snippet:cta_button }} | {{ snippet:cta_button label=\"Give <Now>\" url=Links.Give }} | {{ snippet:wrapper }}",
            context
        );
        Assert.True(ok.IsSuccess);
        Assert.Equal(4, ok.Value.SnippetsExpanded);
        Assert.Equal(
            "<a href=\"https://default.example\">Learn More</a> | <a href=\"https://church.example/give\">Give &lt;Now&gt;</a> | <div><a href=\"https://give.example\">Give Today</a></div>",
            ok.Value.RenderedContent
        );

        var cycle = await engine.RenderAsync("{{ snippet:cycle_a }}");
        Assert.True(cycle.IsFailure);
        Assert.Equal(
            CommunicationTemplateErrors.SnippetCycleDetected.Code,
            cycle.PrimaryError.Code
        );

        var depthCap = await engine.RenderAsync(
            "{{ snippet:wrapper }}",
            options: new MergeRenderOptions(MaxSnippetDepth: 1)
        );
        Assert.True(depthCap.IsFailure);
        Assert.Equal(
            CommunicationTemplateErrors.SnippetDepthExceeded.Code,
            depthCap.PrimaryError.Code
        );

        var unknown = await engine.RenderAsync("{{ snippet:missing_snippet }}");
        Assert.True(unknown.IsFailure);
        Assert.Equal("Communication.UnknownSnippet", unknown.PrimaryError.Code);
    }

    [Fact]
    public async Task Render_Enforces_Syntax_Limits_StrictMode_And_JsonParsing()
    {
        var engine = new SafeMergeTemplateEngine(new InMemorySnippetStore());

        Assert.Equal(
            CommunicationTemplateErrors.TemplateTooLarge.Code,
            (
                await engine.RenderAsync(
                    "123456",
                    options: new MergeRenderOptions(MaxTemplateLength: 4)
                )
            )
                .PrimaryError
                .Code
        );
        Assert.Equal(
            CommunicationTemplateErrors.OutputLimitExceeded.Code,
            (
                await engine.RenderAsync(
                    "hello world",
                    options: new MergeRenderOptions(MaxOutputLength: 5)
                )
            )
                .PrimaryError
                .Code
        );
        Assert.Equal(
            CommunicationTemplateErrors.TokenLimitExceeded.Code,
            (
                await engine.RenderAsync(
                    "{{ A }}{{ B }}",
                    options: new MergeRenderOptions(MaxTokenCount: 1)
                )
            )
                .PrimaryError
                .Code
        );
        Assert.Equal(
            CommunicationTemplateErrors.ConditionalDepthExceeded.Code,
            (
                await engine.RenderAsync(
                    "{{#if A}}{{#if B}}x{{/if}}{{/if}}",
                    options: new MergeRenderOptions(MaxConditionalDepth: 1)
                )
            )
                .PrimaryError
                .Code
        );

        var strictFail = await engine.RenderAsync(
            "Hi {{ Missing.One }} and {{ Missing.One }}",
            options: new MergeRenderOptions(StrictMissingTokens: true)
        );
        Assert.True(strictFail.IsFailure);
        Assert.Equal("Communication.MissingTokens", strictFail.PrimaryError.Code);

        string[] invalidTemplates =
        [
            "{{ unclosed",
            "{{ }}",
            "{{#if A}}missing end",
            "{{#else}}stray else",
            "{{#if A}}a{{#else}}b{{#else}}c{{/if}}",
            "{{/if}}",
            "{{ Name | unknown_filter }}",
            "{{ Name | }}",
            "{{ Name | default:'unclosed }}",
            "{{ 123bad }}",
            "{{ good..bad }}",
            "{{ good$bad }}",
            "{{ snippet: }}",
            "{{ snippet:cta_button badarg }}",
            "{{ snippet:cta_button 1bad=\"x\" }}",
            "{{ snippet:cta_button label=\"unclosed }}",
            "{{ snippet:cta_button label= }}",
        ];

        foreach (var bad in invalidTemplates)
        {
            var res = await engine.RenderAsync(bad);
            Assert.True(res.IsFailure, $"Expected syntax failure for: {bad}");
        }

        var parsedJson = MergeValueResolver.ParseJsonObject(
            /*lang=json,strict*/
            """{"person":{"name":"Alex","age":30,"active":true,"note":null,"tags":["a","b"]}}"""
        );
        Assert.True(parsedJson.IsSuccess);
        var jsonRender = await engine.RenderAsync(
            "{{ person.name }}-{{ person.age }}-{{ person.active }}",
            parsedJson.Value
        );
        Assert.True(jsonRender.IsSuccess);
        Assert.Equal("Alex-30-true", jsonRender.Value.RenderedContent);

        Assert.True(MergeValueResolver.ParseJsonObject("[1,2,3]").IsFailure);
        Assert.True(MergeValueResolver.ParseJsonObject("{not-json").IsFailure);

        using var doc = JsonDocument.Parse("""{"nested":{"value":"ok"}}""");
        var elemCtx = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["doc"] = doc.RootElement.Clone(),
        };
        var elemRender = await engine.RenderAsync("{{ doc.nested.value }}", elemCtx);
        Assert.True(elemRender.IsSuccess);
        Assert.Equal("ok", elemRender.Value.RenderedContent);
    }
}
