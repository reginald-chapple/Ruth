using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Communication.Slices.BulkCommunications;
using Ruth.SharedKernel.Messaging;
using DomainResult = Ruth.SharedKernel.Results.Result;

namespace Ruth.Modules.Communication.Slices.TwoWaySms;

internal static class TwoWaySmsEndpoints
{
    private static readonly JsonSerializerOptions _jsonOptions = CreateJsonOptions();

    public static IEndpointRouteBuilder MapTwoWaySmsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapSmsPipelineEndpoints();

        var inbox = endpoints
            .MapGroup("/api/communication/sms/conversations")
            .WithTags("Communication Two-Way SMS & Keyword Pipelines")
            .RequireAuthorization();

        _ = inbox
            .MapGet("/", ListConversationsAsync)
            .WithName("CommunicationListSmsConversations")
            .WithSummary("List two-way SMS inbox conversation threads")
            .Produces<IReadOnlyList<SmsConversationSummaryResponse>>(StatusCodes.Status200OK);

        _ = inbox
            .MapGet("/thread", GetThreadAsync)
            .WithName("CommunicationGetSmsConversationThread")
            .WithSummary("Get chronological messages in a two-way SMS thread")
            .Produces<IReadOnlyList<SmsConversationMessageResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = inbox
            .MapPost("/reply", ReplyAsync)
            .WithName("CommunicationSendSmsConversationReply")
            .WithSummary("Send a manual 1-to-1 outbound SMS reply in a conversation thread")
            .Produces<SmsConversationMessageResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = inbox
            .MapPost("/mark-read", MarkReadAsync)
            .WithName("CommunicationMarkSmsConversationRead")
            .WithSummary("Mark unread inbound messages in a thread as read")
            .Produces<int>(StatusCodes.Status200OK);

        _ = endpoints
            .MapPost("/api/communication/sms/webhook", IngestInboundSmsAsync)
            .AllowAnonymous()
            .WithTags("Communication Two-Way SMS & Keyword Pipelines")
            .WithName("CommunicationIngestInboundSmsWebhook")
            .WithSummary("Ingest inbound SMS webhook and execute SmsPipeline keyword responders")
            .Produces<InboundSmsProcessResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<SmsConversationSummaryResponse>>, ProblemHttpResult>
    > ListConversationsAsync(
        Guid? systemPhoneNumberId,
        bool? unreadOnly,
        int? limit,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetSmsConversationsQuery,
            IReadOnlyList<SmsConversationSummaryResponse>
        >(
            new GetSmsConversationsQuery(systemPhoneNumberId, unreadOnly ?? false, limit ?? 50),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<SmsConversationMessageResponse>>, ProblemHttpResult>
    > GetThreadAsync(
        Guid systemPhoneNumberId,
        string participantPhoneNumber,
        int? limit,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetSmsConversationThreadQuery,
            IReadOnlyList<SmsConversationMessageResponse>
        >(
            new GetSmsConversationThreadQuery(
                systemPhoneNumberId,
                participantPhoneNumber,
                limit ?? 100
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<SmsConversationMessageResponse>, ProblemHttpResult>
    > ReplyAsync(
        SendSmsConversationReplyCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            SendSmsConversationReplyCommand,
            SmsConversationMessageResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/communication/sms/conversations/thread?systemPhoneNumberId={result.Value.SystemPhoneNumberId:D}",
                result.Value
            )
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<int>, ProblemHttpResult>> MarkReadAsync(
        MarkSmsConversationReadCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<MarkSmsConversationReadCommand, int>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<InboundSmsProcessResponse>, ProblemHttpResult>
    > IngestInboundSmsAsync(
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
            : JsonSerializer.Deserialize<ProcessInboundSmsWebhookCommand>(rawBody, _jsonOptions);
        if (command is null)
        {
            return CommunicationProblemResultMapper.ToProblemResult(
                DomainResult.Failure(BulkCommunicationErrors.SmsMessageBodyInvalid),
                httpContext
            );
        }

        var result = await dispatcher.SendAsync<
            ProcessInboundSmsWebhookCommand,
            InboundSmsProcessResponse
        >(command, cancellationToken);
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
