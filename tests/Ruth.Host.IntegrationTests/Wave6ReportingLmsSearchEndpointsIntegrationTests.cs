using System.Net;
using System.Text.Json;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class Wave6ReportingLmsSearchEndpointsIntegrationTests(
    HostContainersFixture containers
)
{
    private static readonly string[] _blazorRoutes =
    [
        "/reporting/dataviews",
        "/reporting/reports",
        "/reporting/metrics",
        "/reporting/executive",
        "/lms/programs",
        "/lms/grading",
        "/lms/catalog",
        "/search/collections",
    ];

    private static readonly string[] _expectedOpenApiPaths =
    [
        "/api/reporting/dataviews",
        "/api/reporting/reports",
        "/api/reporting/merge-templates",
        "/api/reporting/metrics",
        "/api/reporting/analytics/revenue-impact",
        "/api/reporting/analytics/executive-insights",
        "/api/lms/programs",
        "/api/lms/catalog",
        "/api/search/spotlight",
        "/api/search/public",
    ];

    [Fact]
    public async Task Wave6_S5ToS8_Reporting_Lms_Search_OpenApi_AndBlazorPages_WorkEndToEnd()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = HostWebApplicationFactory.Create(
            isolatedDb,
            containers.RedisConnectionString,
            containers.RustFsEndpoint,
            environment: "Development",
            overrides: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Bootstrap:AutoMigrateAndSeed"] = "true",
                ["OpenApi:Enabled"] = "true",
                ["Cms:InteractionFlushWorkerEnabled"] = "false",
            }
        );
        using var client = factory.CreateNoRedirectClient();

        var token = await Wave4CommerceHttpSeedHelper.OnboardAndLoginAsync(client);
        await VerifyW6S5ReportingHttpAsync(client, token);
        await VerifyW6S6MetricsAndAnalyticsHttpAsync(client, token);
        await VerifyW6S7LmsHttpAsync(client, token);
        await VerifyW6S8SearchHttpAsync(client, token);
        await VerifyOpenApiAndBlazorRoutesAsync(client);
    }

    private static async Task VerifyW6S5ReportingHttpAsync(HttpClient client, string token)
    {
        using var dvResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/reporting/dataviews",
            new
            {
                name = "Host Active Members",
                entityTypeKey = "Ruth.Crm.Person",
                persistedScheduleIntervalMinutes = 30,
                rootFilter = new { expressionType = 1, children = Array.Empty<object>() },
            }
        );
        Assert.Equal(HttpStatusCode.Created, dvResp.StatusCode);
        using var dvDoc = JsonDocument.Parse(await dvResp.Content.ReadAsStringAsync());
        var dataViewId = dvDoc.RootElement.GetProperty("id").GetGuid();

        using var matResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            $"/api/reporting/dataviews/{dataViewId:D}/materialize",
            new { }
        );
        await AssertStatusAsync(matResp, HttpStatusCode.OK);

        using var repResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/reporting/reports",
            new
            {
                name = "Host Directory Export",
                entityTypeKey = "Ruth.Crm.Person",
                dataViewId,
                fields = new[]
                {
                    new
                    {
                        fieldSelectKey = "reporting.field.property",
                        columnHeaderText = "Name",
                        sortOrder = 0,
                        selectionConfigJson = JsonSerializer.Serialize(
                            new { property = "FirstName" }
                        ),
                    },
                },
            }
        );
        await AssertStatusAsync(repResp, HttpStatusCode.Created);
        using var repDoc = JsonDocument.Parse(await repResp.Content.ReadAsStringAsync());
        var reportId = repDoc.RootElement.GetProperty("id").GetGuid();

        using var csvResp = await SendAuthGetAsync(
            client,
            token,
            $"/api/reporting/reports/{reportId:D}/export?format=Csv"
        );
        Assert.Equal(HttpStatusCode.OK, csvResp.StatusCode);
        Assert.Contains(
            "text/csv",
            csvResp.Content.Headers.ContentType?.MediaType,
            StringComparison.OrdinalIgnoreCase
        );

        using var xlsxResp = await SendAuthGetAsync(
            client,
            token,
            $"/api/reporting/reports/{reportId:D}/export?format=Excel"
        );
        Assert.Equal(HttpStatusCode.OK, xlsxResp.StatusCode);
        var xlsxBytes = await xlsxResp.Content.ReadAsByteArrayAsync();
        Assert.True(xlsxBytes.Length > 4 && xlsxBytes[0] == (byte)'P' && xlsxBytes[1] == (byte)'K');

        using var mtResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/reporting/merge-templates",
            new
            {
                name = "Host Welcome Packet",
                format = 0,
                templateBody = "<h1>Welcome {{ Row.Name }}</h1>",
            }
        );
        Assert.Equal(HttpStatusCode.Created, mtResp.StatusCode);
        using var mtDoc = JsonDocument.Parse(await mtResp.Content.ReadAsStringAsync());
        var mergeTemplateId = mtDoc.RootElement.GetProperty("id").GetGuid();

        using var genResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            $"/api/reporting/merge-templates/{mergeTemplateId:D}/generate",
            new { mergeTemplateId, reportId }
        );
        Assert.Equal(HttpStatusCode.OK, genResp.StatusCode);
    }

    private static async Task VerifyW6S6MetricsAndAnalyticsHttpAsync(
        HttpClient client,
        string token
    )
    {
        using var catResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/reporting/metric-categories",
            new { name = "Host Operations" }
        );
        Assert.Equal(HttpStatusCode.Created, catResp.StatusCode);
        using var catDoc = JsonDocument.Parse(await catResp.Content.ReadAsStringAsync());
        var categoryId = catDoc.RootElement.GetProperty("id").GetGuid();

        using var metResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/reporting/metrics",
            new
            {
                title = "Weekly Active Participants",
                metricKey = "host.weekly-active-participants",
                categoryId,
                sourceType = 0,
            }
        );
        Assert.Equal(HttpStatusCode.Created, metResp.StatusCode);
        using var metDoc = JsonDocument.Parse(await metResp.Content.ReadAsStringAsync());
        var metricId = metDoc.RootElement.GetProperty("id").GetGuid();

        using var valResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/reporting/metrics/values/batch",
            new
            {
                entries = new[]
                {
                    new
                    {
                        metricId,
                        metricValueType = 0,
                        yValue = 142m,
                        metricValueDateTimeUtc = DateTimeOffset.UtcNow,
                    },
                },
            }
        );
        Assert.Equal(HttpStatusCode.OK, valResp.StatusCode);

        using var chartResp = await SendAuthGetAsync(
            client,
            token,
            $"/api/reporting/metrics/{metricId:D}/chart?granularity=Month"
        );
        Assert.Equal(HttpStatusCode.OK, chartResp.StatusCode);

        using var revResp = await SendAuthGetAsync(
            client,
            token,
            "/api/reporting/analytics/revenue-impact"
        );
        Assert.Equal(HttpStatusCode.OK, revResp.StatusCode);

        using var execResp = await SendAuthGetAsync(
            client,
            token,
            "/api/reporting/analytics/executive-insights?windowDays=30"
        );
        Assert.Equal(HttpStatusCode.OK, execResp.StatusCode);

        using var starResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/reporting/analytics/star-schema/refresh",
            new { }
        );
        await AssertStatusAsync(starResp, HttpStatusCode.OK);
    }

    private static async Task VerifyW6S7LmsHttpAsync(HttpClient client, string token)
    {
        using var gsResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/lms/grading-systems",
            new
            {
                name = "Host Pass/Fail",
                scales = new[]
                {
                    new
                    {
                        name = "Pass",
                        thresholdPercentage = 70m,
                        isPassing = true,
                    },
                    new
                    {
                        name = "Fail",
                        thresholdPercentage = 0m,
                        isPassing = false,
                    },
                },
            }
        );
        Assert.Equal(HttpStatusCode.Created, gsResp.StatusCode);
        using var gsDoc = JsonDocument.Parse(await gsResp.Content.ReadAsStringAsync());
        var gradingId = gsDoc.RootElement.GetProperty("id").GetGuid();

        using var progResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/lms/programs",
            new
            {
                name = "Host Leadership Academy",
                configurationMode = 0,
                defaultGradingSystemId = gradingId,
                isPublic = true,
            }
        );
        Assert.Equal(HttpStatusCode.Created, progResp.StatusCode);
        using var progDoc = JsonDocument.Parse(await progResp.Content.ReadAsStringAsync());
        var programId = progDoc.RootElement.GetProperty("id").GetGuid();

        using var courseResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/lms/courses",
            new
            {
                learningProgramId = programId,
                name = "Onboarding Orientation",
                courseCode = "ORNT-101",
                isPublic = true,
            }
        );
        Assert.Equal(HttpStatusCode.Created, courseResp.StatusCode);
        using var courseDoc = JsonDocument.Parse(await courseResp.Content.ReadAsStringAsync());
        var courseId = courseDoc.RootElement.GetProperty("id").GetGuid();

        using var classResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/lms/classes",
            new
            {
                learningCourseId = courseId,
                name = "Self-Paced Cohort",
                learningGradingSystemId = gradingId,
                isPublic = true,
            }
        );
        Assert.Equal(HttpStatusCode.Created, classResp.StatusCode);

        // Public catalog must be anonymously accessible
        using var catResp = await client.GetAsync(new Uri("/api/lms/catalog", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, catResp.StatusCode);
        Assert.Contains(
            "Onboarding Orientation",
            await catResp.Content.ReadAsStringAsync(),
            StringComparison.Ordinal
        );
    }

    private static async Task VerifyW6S8SearchHttpAsync(HttpClient client, string token)
    {
        using var colResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/search/collections",
            new
            {
                name = "Host Universal Catalog",
                collectionKey = "host-catalog",
                isPublic = true,
            }
        );
        Assert.Equal(HttpStatusCode.Created, colResp.StatusCode);

        using var reindexResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/search/reindex",
            new { }
        );
        Assert.Equal(HttpStatusCode.OK, reindexResp.StatusCode);

        using var pubSearch = await client.GetAsync(
            new Uri("/api/search/public?q=Onboarding", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, pubSearch.StatusCode);
        Assert.Contains(
            "Onboarding Orientation",
            await pubSearch.Content.ReadAsStringAsync(),
            StringComparison.Ordinal
        );

        using var spotlight = await SendAuthGetAsync(
            client,
            token,
            "/api/search/spotlight?q=Onboarding&limit=10"
        );
        Assert.Equal(HttpStatusCode.OK, spotlight.StatusCode);
        Assert.Contains(
            "Onboarding Orientation",
            await spotlight.Content.ReadAsStringAsync(),
            StringComparison.Ordinal
        );
    }

    private static async Task VerifyOpenApiAndBlazorRoutesAsync(HttpClient client)
    {
        using var openApiResp = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, openApiResp.StatusCode);
        var openApiJson = await openApiResp.Content.ReadAsStringAsync();
        foreach (var path in _expectedOpenApiPaths)
        {
            Assert.Contains(path, openApiJson, StringComparison.Ordinal);
        }

        foreach (var route in _blazorRoutes)
        {
            using var pageResp = await client.GetAsync(new Uri(route, UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, pageResp.StatusCode);
        }
    }

    private static async Task AssertStatusAsync(HttpResponseMessage resp, HttpStatusCode expected)
    {
        if (resp.StatusCode != expected)
        {
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Fail($"Expected {expected}, got {resp.StatusCode}: {body}");
        }
    }

    private static async Task<HttpResponseMessage> SendAuthGetAsync(
        HttpClient client,
        string token,
        string path
    )
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            token
        );
        return await client.SendAsync(req);
    }
}
