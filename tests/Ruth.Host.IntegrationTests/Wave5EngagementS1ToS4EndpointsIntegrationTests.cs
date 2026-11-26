using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class Wave5EngagementS1ToS4EndpointsIntegrationTests(HostContainersFixture containers)
{
    [Fact]
    public async Task Pipelines_Journeys_Streaks_Care_Portfolio_OpenApi_AndBlazorPages_WorkEndToEnd()
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
            }
        );
        using var client = factory.CreateNoRedirectClient();

        var token = await Wave4CommerceHttpSeedHelper.OnboardAndLoginAsync(client);
        var personId = await VerifyPipelinesAndPublicSignupAsync(client, token);
        await VerifyJourneysStreaksAndAchievementsAsync(client, token, personId);
        await VerifySupportRemindersNotificationsAndFollowingAsync(client, token, personId);
        await VerifyPortfolioEndpointsAsync(client, token, personId);
        await VerifyOpenApiAndBlazorRoutesAsync(client);
    }

    private static async Task<Guid> VerifyPipelinesAndPublicSignupAsync(
        HttpClient client,
        string token
    )
    {
        using var pipeResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/engagement/pipelines",
            new
            {
                name = "Community Mentor Intake",
                slug = "community-mentor-intake-pipe",
                stages = new object[]
                {
                    new
                    {
                        name = "New Inquiry",
                        order = 0,
                        isInitial = true,
                    },
                    new
                    {
                        name = "Connected",
                        order = 1,
                        isTerminalCompleted = true,
                    },
                },
            }
        );
        Assert.Equal(HttpStatusCode.Created, pipeResp.StatusCode);
        using var pipeDoc = JsonDocument.Parse(await pipeResp.Content.ReadAsStringAsync());
        var pipeId = pipeDoc.RootElement.GetProperty("id").GetGuid();

        using var oppResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/engagement/pipelines/opportunities",
            new
            {
                pipelineTypeId = pipeId,
                name = "Community Mentor Intake",
                slug = "community-mentor-intake",
                publicSummary = "Mentor new members.",
            }
        );
        Assert.Equal(HttpStatusCode.Created, oppResp.StatusCode);

        using var pubList = await client.GetAsync(
            new Uri("/api/engagement/public/opportunities", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, pubList.StatusCode);

        using var signupResp = await client.PostAsJsonAsync(
            new Uri("/api/engagement/public/signup", UriKind.Relative),
            new
            {
                opportunityIdOrSlug = "community-mentor-intake",
                firstName = "Nadia",
                lastName = "Patel",
                email = "nadia.patel@example.org",
                comments = "Interested in mentoring",
            }
        );
        Assert.Equal(HttpStatusCode.Created, signupResp.StatusCode);
        using var signupDoc = JsonDocument.Parse(await signupResp.Content.ReadAsStringAsync());
        var reqId = signupDoc.RootElement.GetProperty("id").GetGuid();
        var personId = signupDoc.RootElement.GetProperty("requesterPersonId").GetGuid();

        using var connResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            $"/api/engagement/pipelines/requests/{reqId:D}/connect",
            new { requestId = reqId, note = "Connected via API" }
        );
        Assert.Equal(HttpStatusCode.OK, connResp.StatusCode);

        using var boardGet = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/engagement/pipelines/{pipeId:D}/board?includeClosed=true"
        );
        boardGet.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var boardResp = await client.SendAsync(boardGet);
        Assert.Equal(HttpStatusCode.OK, boardResp.StatusCode);

        return personId;
    }

    private static async Task VerifyJourneysStreaksAndAchievementsAsync(
        HttpClient client,
        string token,
        Guid personId
    )
    {
        using var progResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/engagement/journeys/programs",
            new
            {
                name = "Onboarding Track",
                slug = "onboarding-track",
                statuses = new object[]
                {
                    new
                    {
                        name = "Completed",
                        colorHex = "#107C10",
                        isCompleteStatus = true,
                        order = 0,
                    },
                },
                milestoneTypes = new object[]
                {
                    new
                    {
                        name = "Orientation",
                        code = "ORIENT",
                        order = 0,
                    },
                },
            }
        );
        Assert.Equal(HttpStatusCode.Created, progResp.StatusCode);
        using var progDoc = JsonDocument.Parse(await progResp.Content.ReadAsStringAsync());
        var progId = progDoc.RootElement.GetProperty("id").GetGuid();
        var statusId = progDoc.RootElement.GetProperty("statuses")[0].GetProperty("id").GetGuid();
        var msTypeId = progDoc
            .RootElement.GetProperty("milestoneTypes")[0]
            .GetProperty("id")
            .GetGuid();

        using var msResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/engagement/journeys/milestones",
            new
            {
                personId,
                journeyProgramId = progId,
                milestoneTypeId = msTypeId,
                milestoneStatusId = statusId,
                startDate = "2026-11-20",
            }
        );
        Assert.Equal(HttpStatusCode.Created, msResp.StatusCode);

        using var stResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/engagement/streaks/types",
            new
            {
                name = "Weekly Cohort",
                code = "WEEKLY_HOST",
                cadence = "Weekly",
                anchorStartDate = "2026-01-04",
            }
        );
        Assert.Equal(HttpStatusCode.Created, stResp.StatusCode);
        using var stDoc = JsonDocument.Parse(await stResp.Content.ReadAsStringAsync());
        var stId = stDoc.RootElement.GetProperty("id").GetGuid();

        using var recResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/engagement/streaks/record",
            new
            {
                streakTypeId = stId,
                personId,
                engagementDate = "2026-01-11",
            }
        );
        Assert.Equal(HttpStatusCode.OK, recResp.StatusCode);

        using var achResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/engagement/achievements/types",
            new
            {
                name = "First Badge",
                code = "FIRST_BADGE",
                sourceKind = "ManualOrExternal",
                targetCount = 1,
            }
        );
        Assert.Equal(HttpStatusCode.Created, achResp.StatusCode);
    }

    private static async Task VerifySupportRemindersNotificationsAndFollowingAsync(
        HttpClient client,
        string token,
        Guid personId
    )
    {
        using var catResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/engagement/support/categories",
            new { name = "Member Care", slug = "member-care" }
        );
        Assert.Equal(HttpStatusCode.Created, catResp.StatusCode);
        using var catDoc = JsonDocument.Parse(await catResp.Content.ReadAsStringAsync());
        var catId = catDoc.RootElement.GetProperty("id").GetGuid();

        using var subResp = await client.PostAsJsonAsync(
            new Uri("/api/engagement/public/support/submit", UriKind.Relative),
            new
            {
                supportCategoryId = catId,
                firstName = "Sam",
                lastName = "Carter",
                requestText = "Community ride assistance needed",
                visibilityLevel = "PublicCommunityWall",
            }
        );
        Assert.Equal(HttpStatusCode.Created, subResp.StatusCode);
        using var subDoc = JsonDocument.Parse(await subResp.Content.ReadAsStringAsync());
        var supportId = subDoc.RootElement.GetProperty("id").GetGuid();

        using var modResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            $"/api/engagement/support/{supportId:D}/moderate",
            new { supportRequestId = supportId, decision = "Approve" }
        );
        Assert.Equal(HttpStatusCode.OK, modResp.StatusCode);

        using var wallResp = await client.GetAsync(
            new Uri("/api/engagement/public/support/wall", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, wallResp.StatusCode);

        using var folResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/engagement/following/toggle",
            new
            {
                followerPersonId = personId,
                entityTypeKey = "Ruth.Crm.Person",
                entityId = Guid.CreateVersion7(),
                entityDisplayName = "Cohort Member",
            }
        );
        Assert.Equal(HttpStatusCode.OK, folResp.StatusCode);
    }

    private static async Task VerifyPortfolioEndpointsAsync(
        HttpClient client,
        string token,
        Guid constituentId
    )
    {
        var stewardId = Guid.CreateVersion7();
        using var contactResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/engagement/portfolio/contacts",
            new
            {
                ownerPersonId = stewardId,
                constituentPersonId = constituentId,
                portfolioRole = "Steward",
                outreachCadenceDays = 30,
            }
        );
        Assert.Equal(HttpStatusCode.Created, contactResp.StatusCode);

        using var qReq = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/engagement/portfolio/queue?ownerPersonId={stewardId:D}"
        );
        qReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var qResp = await client.SendAsync(qReq);
        Assert.Equal(HttpStatusCode.OK, qResp.StatusCode);
        using var qDoc = JsonDocument.Parse(await qResp.Content.ReadAsStringAsync());
        var tpId = qDoc.RootElement.GetProperty("outreachQueue")[0].GetProperty("id").GetGuid();

        using var compResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            $"/api/engagement/portfolio/touchpoints/{tpId:D}/complete",
            new
            {
                touchpointId = tpId,
                channelUsed = "PhoneCall",
                outcomeSentiment = "Positive",
                summary = "Connected by phone",
            }
        );
        Assert.Equal(HttpStatusCode.OK, compResp.StatusCode);
    }

    private static async Task VerifyOpenApiAndBlazorRoutesAsync(HttpClient client)
    {
        using var openApiResp = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, openApiResp.StatusCode);
        var openApiText = await openApiResp.Content.ReadAsStringAsync();
        Assert.Contains("/api/engagement/public/signup", openApiText, StringComparison.Ordinal);
        Assert.Contains("/api/engagement/portfolio/queue", openApiText, StringComparison.Ordinal);

        foreach (
            var route in new[]
            {
                "/engagement/pipelines",
                "/engagement/opportunities",
                "/engagement/opportunities/community-mentor-intake",
                "/engagement/journeys",
                "/engagement/streaks",
                "/engagement/care",
                "/engagement/support",
                "/engagement/notifications",
                "/engagement/portfolio",
            }
        )
        {
            using var response = await client.GetAsync(new Uri(route, UriKind.Relative));
            var html = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            Assert.DoesNotContain(
                "An unhandled error has occurred",
                html,
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}
