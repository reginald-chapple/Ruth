using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Engine;
using Xunit;

namespace Ruth.Modules.Cms.Tests.Domain;

public sealed class W6S1CmsEngineDomainTests
{
    [Fact]
    public void CmsTheme_ValidatesCssTokens_AndRendersSanitizedCssBundle()
    {
        var bad = CmsTheme.Create(
            "Bad",
            "bad",
            cssCustomProperties: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["color"] = "#fff",
            }
        );
        Assert.True(bad.IsFailure);

        var good = CmsTheme.Create(
            "Portal Modern",
            "Portal Modern!",
            "Primary portal tokens",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["--cms-accent"] = "#0F6CBD;}</style>",
                ["--cms-radius"] = "8px",
            },
            customCss: ".hero { padding: 1rem; }"
        );
        Assert.True(good.IsSuccess);
        Assert.Equal("portal-modern", good.Value.Slug);

        var css = good.Value.RenderCssBundle();
        Assert.Contains("--cms-accent: #0F6CBD;", css, StringComparison.Ordinal);
        Assert.DoesNotContain("</style", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".hero { padding: 1rem; }", css, StringComparison.Ordinal);
    }

    [Fact]
    public void CmsSite_Page_Block_AndHtmlContent_EnforceDomainInvariants()
    {
        var site = CmsSite.Create("Member Portal", "member-portal").Value;
        Assert.True(site.AddDomain("https://Portal.Org.Local:443/path").IsSuccess);
        Assert.Equal("portal.org.local", site.Domains.Single().Domain);
        Assert.True(site.AddDomain("portal.org.local").IsFailure);

        var layout = CmsLayout.Create(site.Id, "Two Col", "two-col", zones: ["Hero", "Main"]).Value;
        Assert.True(layout.HasZone("hero"));
        Assert.False(layout.HasZone("sidebar"));

        var page = CmsPage.Create(site.Id, layout.Id, "Profile", "Member Profile").Value;
        Assert.True(page.AddRoute("/members/{memberId}/profile").IsSuccess);
        Assert.True(page.AddRoute("members/{memberId}/profile").IsFailure);
        Assert.True(page.AddContext("Person", "memberId").IsSuccess);
        Assert.True(page.AddContext("Person", "memberId").IsFailure);

        var bt = CmsBlockType.Create("cms.html-content", "HTML Content").Value;
        Assert.True(CmsBlock.Create(bt.Id, "Main", "Orphan").IsFailure);
        Assert.True(
            CmsBlock.Create(bt.Id, "Main", "Dual", pageId: page.Id, siteId: site.Id).IsFailure
        );

        var block = CmsBlock.Create(bt.Id, "Main", "Bio Block", pageId: page.Id).Value;
        var now = DateTimeOffset.UtcNow;
        var html = HtmlContent
            .Create(
                block.Id,
                1,
                "<p>Hello</p>",
                startDateTimeUtc: now.AddMinutes(-10),
                expireDateTimeUtc: now.AddHours(1)
            )
            .Value;

        Assert.False(html.IsActiveAt(now));
        var approver = Guid.CreateVersion7();
        html.SetApproval(isApproved: true, approver, now);
        Assert.True(html.IsActiveAt(now));
        Assert.False(html.IsActiveAt(now.AddHours(2)));

        var survivor = Guid.CreateVersion7();
        Assert.True(html.RepointApprovedByPerson(approver, survivor));
        Assert.Equal(survivor, html.ApprovedByPersonId);

        Assert.True(
            block
                .UpdateConfiguration(
                    "Sidebar",
                    "Updated Bio Block",
                    order: 2,
                    isActive: true,
                    preHtml: "<section>",
                    postHtml: "</section>",
                    cssClass: "cms-bio",
                    settings: new Dictionary<string, string?>(StringComparer.Ordinal)
                    {
                        ["ShowTitle"] = "true",
                    }
                )
                .IsSuccess
        );
        Assert.Equal("Sidebar", block.Zone);
        Assert.Equal(2, block.Order);
        Assert.Equal("true", block.Settings["ShowTitle"]);
        Assert.True(
            block
                .UpdateConfiguration(
                    "",
                    "Bad",
                    order: 0,
                    isActive: true,
                    preHtml: null,
                    postHtml: null,
                    cssClass: null,
                    settings: null
                )
                .IsFailure
        );

        Assert.Equal("cms.my-block", CmsTextNormalizer.NormalizeKey("  CMS.My Block! "));
        Assert.Equal(string.Empty, CmsTextNormalizer.NormalizeKey("   "));
        Assert.False(CmsTextNormalizer.IsValidCssCustomPropertyName("--"));
        Assert.False(CmsTextNormalizer.IsValidCssCustomPropertyName("--bad space"));
        Assert.False(CmsTextNormalizer.IsValidCssCustomPropertyName(null));
    }

    [Fact]
    public void CmsRouteMatcher_MatchesStaticAndParameterizedRoutes_AndExtractsContexts()
    {
        var rootMatch = CmsRouteMatcher.Match("", "/");
        Assert.True(rootMatch.IsMatch);
        Assert.Equal(1000, rootMatch.SpecificityScore);

        var staticMatch = CmsRouteMatcher.Match("events/featured", "/events/featured");
        var paramMatch = CmsRouteMatcher.Match("events/{slug}", "/events/featured");
        Assert.True(staticMatch.IsMatch);
        Assert.True(paramMatch.IsMatch);
        Assert.True(staticMatch.SpecificityScore > paramMatch.SpecificityScore);
        Assert.Equal("featured", paramMatch.Parameters["slug"]);

        var mismatch = CmsRouteMatcher.Match("events/{slug}", "/groups/123");
        Assert.False(mismatch.IsMatch);

        var page = CmsPage
            .Create(CmsSiteId.Create(), CmsLayoutId.Create(), "Detail", "Detail")
            .Value;
        _ = page.AddContext("Event", "slug");
        _ = page.AddContext("Branch", "branchId");

        var contexts = CmsRouteMatcher.ExtractPageContexts(
            page.Contexts,
            paramMatch.Parameters,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["branchId"] = "br-1",
            }
        );
        Assert.Equal("featured", contexts["Event"]);
        Assert.Equal("br-1", contexts["Branch"]);
    }

    [Fact]
    public void PageShortLink_ValidatesUrls_AppendsUtm_AndQrGeneratorProducesValidSvg()
    {
        var siteId = CmsSiteId.Create();
        Assert.True(PageShortLink.Create(siteId, "bad", "javascript:alert(1)").IsFailure);
        Assert.True(PageShortLink.Create(siteId, "bad2", "//evil.example/phish").IsFailure);

        var link = PageShortLink
            .Create(
                siteId,
                "Spring Gala!",
                "https://portal.ruth.local/events?id=42",
                utmSource: "newsletter",
                utmMedium: "email",
                utmCampaign: "spring"
            )
            .Value;

        Assert.Equal("spring-gala", link.Token);
        var resolved = link.BuildResolvedTargetUrl();
        Assert.Contains(
            "&utm_source=newsletter&utm_medium=email&utm_campaign=spring",
            resolved,
            StringComparison.Ordinal
        );

        link.RecordClick(DateTimeOffset.UtcNow);
        Assert.Equal(1, link.ClickCount);

        var shortQr = QrCodeSvgGenerator.GenerateSvg("https://ruth.local/go/spring-gala");
        Assert.StartsWith("<svg ", shortQr, StringComparison.Ordinal);
        Assert.EndsWith("</svg>", shortQr, StringComparison.Ordinal);
        Assert.Contains("viewBox=\"0 0 ", shortQr, StringComparison.Ordinal);

        var longQr = QrCodeSvgGenerator.GenerateSvg(
            "https://portal.ruth.local/go/longer-shortlink-token-with-campaign-tracking-parameters-1234567890"
        );
        Assert.StartsWith("<svg ", longQr, StringComparison.Ordinal);
        Assert.Contains("M4,4h1v1h-1z", longQr, StringComparison.Ordinal);
    }

    [Fact]
    public void RequestFilterEvaluator_EvaluatesAllRuleKindsAndModesAccurately()
    {
        var wednesday1430 = new DateTimeOffset(2026, 11, 25, 14, 30, 0, TimeSpan.Zero);

        var allFilter = RequestFilter
            .Create(
                "Mobile Office Visitor",
                "mobile-office",
                filterMode: RequestFilterMode.AllMatch,
                rules:
                [
                    new(
                        RequestFilterRuleKind.QueryString,
                        RequestFilterComparison.Equals,
                        "ref",
                        "campaign"
                    ),
                    new(
                        RequestFilterRuleKind.Cookie,
                        RequestFilterComparison.Exists,
                        "ruth_visitor"
                    ),
                    new(
                        RequestFilterRuleKind.Header,
                        RequestFilterComparison.Contains,
                        "Accept-Language",
                        "en"
                    ),
                    new(
                        RequestFilterRuleKind.UserAgentDevice,
                        RequestFilterComparison.Equals,
                        "device",
                        "Mobile"
                    ),
                    new(
                        RequestFilterRuleKind.HostDomain,
                        RequestFilterComparison.StartsWith,
                        "host",
                        "portal."
                    ),
                    new(
                        RequestFilterRuleKind.IpAddressRange,
                        RequestFilterComparison.InRange,
                        "ip",
                        "198.51.100.0/24"
                    ),
                    new(
                        RequestFilterRuleKind.TimeOfDay,
                        RequestFilterComparison.InRange,
                        "time",
                        "09:00-17:00"
                    ),
                    new(
                        RequestFilterRuleKind.DayOfWeek,
                        RequestFilterComparison.Equals,
                        "day",
                        "Wednesday,Thursday"
                    ),
                ]
            )
            .Value;

        var matchingCtx = new RequestEvaluationContext(
            Host: "portal.ruth.local:443",
            ClientIpAddress: "198.51.100.42",
            UserAgent: "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) Mobile",
            QueryParameters: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ref"] = "campaign",
            },
            Cookies: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ruth_visitor"] = "abc",
            },
            Headers: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Accept-Language"] = "en-US",
            },
            EvaluatedAtUtc: wednesday1430
        );

        var evalMatch = RequestFilterEvaluator.Evaluate(allFilter, matchingCtx, wednesday1430);
        Assert.True(evalMatch.IsMatch);
        Assert.Equal(8, evalMatch.MatchedRuleCount);

        var nonMatchingCtx = matchingCtx with { ClientIpAddress = "203.0.113.9" };
        var evalMiss = RequestFilterEvaluator.Evaluate(allFilter, nonMatchingCtx, wednesday1430);
        Assert.False(evalMiss.IsMatch);
        Assert.Equal(7, evalMiss.MatchedRuleCount);

        var anyFilter = RequestFilter
            .Create(
                "Overnight TV Or Missing Cookie",
                "tv-or-overnight",
                filterMode: RequestFilterMode.AnyMatch,
                rules:
                [
                    new(
                        RequestFilterRuleKind.UserAgentDevice,
                        RequestFilterComparison.Equals,
                        "device",
                        "Tv"
                    ),
                    new(
                        RequestFilterRuleKind.TimeOfDay,
                        RequestFilterComparison.InRange,
                        "time",
                        "22:00-06:00"
                    ),
                    new(RequestFilterRuleKind.Cookie, RequestFilterComparison.NotExists, "opt_out"),
                ]
            )
            .Value;

        var tvEval = RequestFilterEvaluator.Evaluate(
            anyFilter,
            new RequestEvaluationContext(UserAgent: "Roku/DVP-12.0", EvaluatedAtUtc: wednesday1430),
            wednesday1430
        );
        Assert.True(tvEval.IsMatch);
        Assert.Equal(
            "Tablet",
            RequestFilterEvaluator.ClassifyDeviceType("Mozilla/5.0 (iPad; CPU OS 18_0)")
        );
        Assert.Equal("Desktop", RequestFilterEvaluator.ClassifyDeviceType(userAgent: null));

        var edgeFilter = RequestFilter
            .Create(
                "Edge Comparators",
                "edge-comparators",
                filterMode: RequestFilterMode.AllMatch,
                rules:
                [
                    new(
                        RequestFilterRuleKind.IpAddressRange,
                        RequestFilterComparison.InRange,
                        "ip",
                        "2001:db8::/32"
                    ),
                    new(
                        RequestFilterRuleKind.HostDomain,
                        RequestFilterComparison.Contains,
                        "host",
                        ".org"
                    ),
                    new(
                        RequestFilterRuleKind.QueryString,
                        RequestFilterComparison.NotEquals,
                        "MODE",
                        "debug"
                    ),
                    new(
                        RequestFilterRuleKind.TimeOfDay,
                        RequestFilterComparison.InRange,
                        "time",
                        "22:00-06:00"
                    ),
                ]
            )
            .Value;

        var lateNight = new DateTimeOffset(2026, 11, 25, 23, 30, 0, TimeSpan.Zero);
        var ipv6Ctx = new RequestEvaluationContext(
            Host: "portal.ruth.org",
            ClientIpAddress: "2001:db8::1",
            QueryParameters: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["mode"] = "prod",
            },
            EvaluatedAtUtc: lateNight
        );
        Assert.True(RequestFilterEvaluator.Evaluate(edgeFilter, ipv6Ctx, lateNight).IsMatch);

        var exactIpFilter = RequestFilter
            .Create(
                "Exact IP",
                "exact-ip",
                rules:
                [
                    new(
                        RequestFilterRuleKind.IpAddressRange,
                        RequestFilterComparison.Equals,
                        "ip",
                        "198.51.100.7"
                    ),
                ]
            )
            .Value;
        Assert.True(
            RequestFilterEvaluator
                .Evaluate(
                    exactIpFilter,
                    new RequestEvaluationContext(ClientIpAddress: "198.51.100.7"),
                    lateNight
                )
                .IsMatch
        );
        Assert.False(
            RequestFilterEvaluator
                .Evaluate(
                    exactIpFilter,
                    new RequestEvaluationContext(ClientIpAddress: "not-an-ip"),
                    lateNight
                )
                .IsMatch
        );
    }
}
