using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using DomainResult = Ruth.SharedKernel.Results.Result;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal static class CommunicationWebhookAndPreferenceEndpoints
{
    private static readonly JsonSerializerOptions _jsonOptions = CreateJsonOptions();

    public static IEndpointRouteBuilder MapCommunicationWebhookAndPreferenceEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        _ = endpoints
            .MapPost("/api/communication/webhooks/events", IngestWebhookEventsAsync)
            .AllowAnonymous()
            .WithTags("Communication Webhooks & Preferences")
            .WithName("CommunicationIngestWebhookEvents")
            .WithSummary("Ingest provider bounce, open, click, complaint, and unsubscribe webhooks")
            .Produces<CommunicationWebhookProcessResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        var prefs = endpoints
            .MapGroup("/api/communication/preferences")
            .WithTags("Communication Webhooks & Preferences")
            .AllowAnonymous();

        _ = prefs
            .MapGet("/", GetPreferencesAsync)
            .WithName("CommunicationGetRecipientPreferences")
            .WithSummary("Get channel and topic opt-out preferences by signed token or staff query")
            .Produces<CommunicationPreferenceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        _ = prefs
            .MapPut("/", UpdatePreferencesAsync)
            .WithName("CommunicationUpdateRecipientPreferences")
            .WithSummary("Update global and topic opt-out preferences")
            .Produces<CommunicationPreferenceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        _ = prefs
            .MapPost("/unsubscribe", OneClickUnsubscribeAsync)
            .WithName("CommunicationOneClickUnsubscribe")
            .WithSummary("RFC 8058 one-click unsubscribe by signed token")
            .Produces<CommunicationPreferenceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        _ = prefs
            .MapGet("/unsubscribe-link", UnsubscribeByLinkAsync)
            .WithName("CommunicationOneClickUnsubscribeLink")
            .WithSummary("Browser GET unsubscribe link handler")
            .Produces<CommunicationPreferenceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<
        Results<Ok<CommunicationWebhookProcessResponse>, ProblemHttpResult>
    > IngestWebhookEventsAsync(
        HttpContext httpContext,
        IOptions<CommunicationBulkOptions> bulkOptions,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken
    )
    {
        using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        var signature = httpContext
            .Request.Headers[CommunicationWebhookSignatureVerifier.HeaderName]
            .FirstOrDefault();

        if (
            !CommunicationWebhookSignatureVerifier.VerifyIfConfigured(
                rawBody,
                signature,
                bulkOptions.Value.WebhookSigningSecret
            )
        )
        {
            return CommunicationProblemResultMapper.ToProblemResult(
                DomainResult.Failure(BulkCommunicationErrors.WebhookSignatureInvalid),
                httpContext
            );
        }

        var command = string.IsNullOrWhiteSpace(rawBody)
            ? null
            : JsonSerializer.Deserialize<ProcessCommunicationWebhooksCommand>(
                rawBody,
                _jsonOptions
            );
        if (command is null)
        {
            return CommunicationProblemResultMapper.ToProblemResult(
                DomainResult.Failure(BulkCommunicationErrors.WebhookEventsEmpty),
                httpContext
            );
        }

        var result = await dispatcher.SendAsync<
            ProcessCommunicationWebhooksCommand,
            CommunicationWebhookProcessResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CommunicationPreferenceResponse>, ProblemHttpResult>
    > GetPreferencesAsync(
        string? token,
        string? destinationAddress,
        CommunicationChannel? channel,
        Guid? personId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(token) && httpContext.User.Identity?.IsAuthenticated != true)
        {
            return CommunicationProblemResultMapper.ToProblemResult(
                DomainResult.Failure(BulkCommunicationErrors.PreferenceTokenInvalid),
                httpContext
            );
        }

        var result = await dispatcher.QueryAsync<
            GetRecipientPreferencesQuery,
            CommunicationPreferenceResponse
        >(
            new GetRecipientPreferencesQuery(
                token,
                destinationAddress,
                channel ?? CommunicationChannel.Email,
                personId
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CommunicationPreferenceResponse>, ProblemHttpResult>
    > UpdatePreferencesAsync(
        UpdateRecipientPreferencesCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        if (
            string.IsNullOrWhiteSpace(command.PreferenceToken)
            && httpContext.User.Identity?.IsAuthenticated != true
        )
        {
            return CommunicationProblemResultMapper.ToProblemResult(
                DomainResult.Failure(BulkCommunicationErrors.PreferenceTokenInvalid),
                httpContext
            );
        }

        var result = await dispatcher.SendAsync<
            UpdateRecipientPreferencesCommand,
            CommunicationPreferenceResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CommunicationPreferenceResponse>, ProblemHttpResult>
    > OneClickUnsubscribeAsync(
        OneClickUnsubscribeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            OneClickUnsubscribeCommand,
            CommunicationPreferenceResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CommunicationPreferenceResponse>, ProblemHttpResult>
    > UnsubscribeByLinkAsync(
        string? token,
        string? topic,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            OneClickUnsubscribeCommand,
            CommunicationPreferenceResponse
        >(new OneClickUnsubscribeCommand(token ?? string.Empty, topic), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        opts.Converters.Add(new JsonStringEnumConverter());
        return opts;
    }
}
