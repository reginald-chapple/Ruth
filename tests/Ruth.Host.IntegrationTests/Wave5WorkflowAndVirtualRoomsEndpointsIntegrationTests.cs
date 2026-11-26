using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ruth.Host.IntegrationTests.Fixtures;
using Ruth.Modules.VirtualRooms.Contracts.Providers;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class Wave5WorkflowAndVirtualRoomsEndpointsIntegrationTests(
    HostContainersFixture containers
)
{
    private const string DevLiveKitKey = "devkey";
    private const string DevLiveKitSecret = "ruth-inmemory-livekit-dev-signing-secret-32chars!";

    [Fact]
    public async Task Workflows_Forms_VirtualRooms_LiveKitWebhooks_OpenApi_AndBlazorPages_WorkEndToEnd()
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
                ["Workflow:BackgroundWorkerEnabled"] = "false",
                ["VirtualRooms:ActiveProvider"] = "in-memory",
            }
        );
        using var client = factory.CreateNoRedirectClient();

        var token = await Wave4CommerceHttpSeedHelper.OnboardAndLoginAsync(client);
        await VerifyWorkflowEngineAndTriggersHttpAsync(client, token);
        await VerifyVirtualRoomsAndSignedWebhooksHttpAsync(client, token);
        await VerifyOpenApiAndWave5BlazorRoutesAsync(client);
    }

    private static async Task VerifyWorkflowEngineAndTriggersHttpAsync(
        HttpClient client,
        string token
    )
    {
        using var catReq = new HttpRequestMessage(HttpMethod.Get, "/api/workflows/actions/catalog");
        catReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var catResp = await client.SendAsync(catReq);
        Assert.Equal(HttpStatusCode.OK, catResp.StatusCode);

        using var typeResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/workflows/types",
            new
            {
                name = "Host Grant Review",
                slug = "host-grant-review",
                workTerm = "Grant",
                workflowIdPrefix = "HGR-",
                activities = new object[]
                {
                    new
                    {
                        key = "intake",
                        name = "Intake",
                        order = 0,
                        isActivatedWithWorkflow = true,
                        actions = new object[]
                        {
                            new
                            {
                                key = "entry-step",
                                name = "Review Entry",
                                actionPluginKey = "workflow.forms.user-entry",
                                order = 0,
                            },
                        },
                    },
                    new
                    {
                        key = "approved",
                        name = "Approved",
                        order = 1,
                        actions = new object[]
                        {
                            new
                            {
                                key = "complete",
                                name = "Complete",
                                actionPluginKey = "workflow.control.set-status",
                                order = 0,
                                configuration = new
                                {
                                    StateLabel = "Approved",
                                    CompleteWorkflow = "true",
                                },
                            },
                        },
                    },
                },
            }
        );
        Assert.Equal(HttpStatusCode.Created, typeResp.StatusCode);
        using var typeDoc = JsonDocument.Parse(await typeResp.Content.ReadAsStringAsync());
        var wfTypeId = typeDoc.RootElement.GetProperty("id").GetGuid();
        var actionTypeId = typeDoc
            .RootElement.GetProperty("activities")[0]
            .GetProperty("actions")[0]
            .GetProperty("id")
            .GetGuid();

        using var tmplResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/workflows/forms/templates",
            new
            {
                name = "Host Assistance Template",
                slug = "host-assistance-template",
                confirmationTitle = "Received",
                linkedWorkflowTypeId = wfTypeId,
            }
        );
        Assert.Equal(HttpStatusCode.Created, tmplResp.StatusCode);
        using var tmplDoc = JsonDocument.Parse(await tmplResp.Content.ReadAsStringAsync());
        var tmplId = tmplDoc.RootElement.GetProperty("id").GetGuid();

        using var formResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Put,
            $"/api/workflows/forms/actions/{actionTypeId:D}",
            new
            {
                formBuilderTemplateId = tmplId,
                fields = new object[]
                {
                    new
                    {
                        attributeKey = "Summary",
                        label = "Summary",
                        fieldTypeKey = "text",
                        order = 0,
                        isRequired = true,
                    },
                },
                buttons = new object[]
                {
                    new
                    {
                        buttonKey = "approve",
                        label = "Approve",
                        targetActivityKey = "approved",
                        order = 0,
                    },
                },
            }
        );
        Assert.Equal(HttpStatusCode.OK, formResp.StatusCode);

        using var launchResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/workflows/instances/launch",
            new { workflowTypeIdOrSlug = "host-grant-review", runImmediately = true }
        );
        Assert.Equal(HttpStatusCode.Created, launchResp.StatusCode);
        using var launchDoc = JsonDocument.Parse(await launchResp.Content.ReadAsStringAsync());
        var wfId = launchDoc.RootElement.GetProperty("id").GetGuid();

        using var entryGet = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/workflows/forms/entry/{wfId:D}"
        );
        entryGet.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var entryResp = await client.SendAsync(entryGet);
        Assert.Equal(HttpStatusCode.OK, entryResp.StatusCode);
        using var entryDoc = JsonDocument.Parse(await entryResp.Content.ReadAsStringAsync());
        var waitingActionId = entryDoc.RootElement.GetProperty("actionId").GetGuid();

        using var submitResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            $"/api/workflows/forms/entry/{wfId:D}/submit",
            new
            {
                workflowId = wfId,
                actionId = waitingActionId,
                selectedButtonKey = "approve",
                fieldValues = new { Summary = "Approved via HTTP test" },
            }
        );
        Assert.Equal(HttpStatusCode.OK, submitResp.StatusCode);

        using var publicSubmit = await client.PostAsJsonAsync(
            new Uri("/api/workflows/public/forms/submit", UriKind.Relative),
            new
            {
                templateIdOrSlug = "host-assistance-template",
                selectedButtonKey = "approve",
                fieldValues = new { Summary = "Public portal entry" },
                submitterFirstName = "Avery",
                submitterLastName = "Chen",
            }
        );
        Assert.Equal(HttpStatusCode.Created, publicSubmit.StatusCode);
    }

    private static async Task VerifyVirtualRoomsAndSignedWebhooksHttpAsync(
        HttpClient client,
        string token
    )
    {
        using var roomResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/virtual-rooms",
            new
            {
                name = "Host Cohort Stage",
                slug = "host-cohort-stage",
                contextKind = "GroupMeeting",
                minimumAttendanceDwellSeconds = 60,
            }
        );
        Assert.Equal(HttpStatusCode.Created, roomResp.StatusCode);
        using var roomDoc = JsonDocument.Parse(await roomResp.Content.ReadAsStringAsync());
        var lkRoomName = roomDoc.RootElement.GetProperty("liveKitRoomName").GetString()
            ?? string.Empty;

        using var joinResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/virtual-rooms/host-cohort-stage/join-token",
            new
            {
                roomIdOrSlug = "host-cohort-stage",
                displayName = "Host Coordinator",
                ttlMinutes = 30,
            }
        );
        Assert.Equal(HttpStatusCode.OK, joinResp.StatusCode);
        using var joinDoc = JsonDocument.Parse(await joinResp.Content.ReadAsStringAsync());
        var sessionId = joinDoc.RootElement.GetProperty("sessionId").GetGuid();
        Assert.False(
            string.IsNullOrWhiteSpace(joinDoc.RootElement.GetProperty("accessToken").GetString())
        );

        using var recResp = await Wave4CommerceHttpSeedHelper.SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            $"/api/virtual-rooms/sessions/{sessionId:D}/recordings/start",
            new { sessionId }
        );
        Assert.Equal(HttpStatusCode.OK, recResp.StatusCode);
        using var recDoc = JsonDocument.Parse(await recResp.Content.ReadAsStringAsync());
        var egressId = recDoc.RootElement.GetProperty("egressId").GetString() ?? string.Empty;
        var objKey = recDoc.RootElement.GetProperty("storageObjectKey").GetString() ?? string.Empty;

        using var unsignedResp = await client.PostAsJsonAsync(
            new Uri("/api/virtual-rooms/webhooks/livekit", UriKind.Relative),
            new { @event = "room_started" }
        );
        Assert.Equal(HttpStatusCode.Unauthorized, unsignedResp.StatusCode);

        var egressJson = JsonSerializer.Serialize(
            new
            {
                @event = "egress_ended",
                egressInfo = new
                {
                    egressId,
                    roomName = lkRoomName,
                    fileResults = new object[]
                    {
                        new
                        {
                            filename = objKey,
                            size = 524288L,
                            duration = 120000000000L,
                        },
                    },
                },
            }
        );
        var webhookJwt = LiveKitJwtAuthenticator.SignWebhookPayload(
            DevLiveKitKey,
            DevLiveKitSecret,
            Encoding.UTF8.GetBytes(egressJson),
            DateTimeOffset.UtcNow
        );

        using var signedReq = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/virtual-rooms/webhooks/livekit"
        )
        {
            Content = new StringContent(egressJson, Encoding.UTF8, "application/json"),
        };
        signedReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", webhookJwt);
        using var signedResp = await client.SendAsync(signedReq);
        Assert.Equal(HttpStatusCode.OK, signedResp.StatusCode);
    }

    private static async Task VerifyOpenApiAndWave5BlazorRoutesAsync(HttpClient client)
    {
        using var openApiResp = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, openApiResp.StatusCode);
        var openApiText = await openApiResp.Content.ReadAsStringAsync();
        Assert.Contains("/api/workflows/types", openApiText, StringComparison.Ordinal);
        Assert.Contains("/api/workflows/forms/tasks", openApiText, StringComparison.Ordinal);
        Assert.Contains("/api/virtual-rooms", openApiText, StringComparison.Ordinal);
        Assert.Contains(
            "/api/virtual-rooms/webhooks/livekit",
            openApiText,
            StringComparison.Ordinal
        );

        foreach (
            var route in new[]
            {
                "/workflow/definitions",
                "/workflow/form-builder",
                "/workflow/tasks",
                "/virtual-rooms",
                "/virtual-rooms/stage",
                "/virtual-rooms/stage/host-cohort-stage",
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
