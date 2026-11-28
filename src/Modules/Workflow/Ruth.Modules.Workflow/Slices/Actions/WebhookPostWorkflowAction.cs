using System.Globalization;
using System.Net.Http.Json;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Workflow.Slices.Actions;

internal sealed class WebhookPostWorkflowAction(IHttpClientFactory httpClientFactory)
    : IWorkflowAction
{
    public const string HttpClientName = "WorkflowWebhookClient";
    public const string Key = "workflow.integrations.webhook-post";

    public string ActionKey => Key;
    public string DisplayName => "HTTP Webhook JSON POST";
    public string Category => "Integrations";
    public string Description =>
        "Posts a JSON payload of workflow metadata and attributes to Url and stores status in ResponseStatusAttributeKey.";

    public async Task<Result<WorkflowActionDirective>> ExecuteAsync(
        WorkflowActionExecutionContext context,
        CancellationToken cancellationToken = default
    )
    {
        var cfg = context.Configuration;
        if (!TryParseHttpUri(cfg, out var uri))
        {
            return Result.Failure<WorkflowActionDirective>(
                DomainError.Validation(
                    "Workflow.WebhookInvalidUrl",
                    "Webhook action requires an absolute HTTP or HTTPS Url."
                )
            );
        }

        try
        {
            return await SendWebhookCoreAsync(uri, context, cancellationToken);
        }
        catch (OutboundSsrfBlockedException ex)
        {
            return Result.Failure<WorkflowActionDirective>(
                DomainError.Validation("Workflow.WebhookSsrfBlocked", ex.Message)
            );
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<WorkflowActionDirective>(
                DomainError.Failure(
                    "Workflow.WebhookTransportFailed",
                    $"Webhook POST to {uri.Host} failed: {ex.Message}"
                )
            );
        }
    }

    private async Task<Result<WorkflowActionDirective>> SendWebhookCoreAsync(
        Uri uri,
        WorkflowActionExecutionContext context,
        CancellationToken cancellationToken
    )
    {
        var cfg = context.Configuration;
        OutboundAddressGuard.EnsureAllowedUri(uri);
        using var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.PostAsJsonAsync(
            uri,
            BuildPayload(context),
            cancellationToken
        );
        var statusCode = ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);

        if (!response.IsSuccessStatusCode && !AllowNonSuccess(cfg))
        {
            return Result.Failure<WorkflowActionDirective>(
                DomainError.Failure(
                    "Workflow.WebhookFailed",
                    $"Webhook POST to {uri.Host} returned HTTP {statusCode}."
                )
            );
        }

        var statusAttr =
            cfg.TryGetValue("ResponseStatusAttributeKey", out var sk)
            && !string.IsNullOrWhiteSpace(sk)
                ? sk.Trim()
                : "WebhookLastStatusCode";
        var updates = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [statusAttr] = statusCode,
        };

        return Result.Success(
            new WorkflowActionDirective(
                WorkflowActionOutcomeKind.Continue,
                $"Posted webhook to {uri.Host} (HTTP {statusCode}).",
                UpdatedWorkflowAttributes: updates
            )
        );
    }

    private static bool TryParseHttpUri(IReadOnlyDictionary<string, string?> cfg, out Uri uri)
    {
        uri = null!;
        if (
            !cfg.TryGetValue("Url", out var rawUrl)
            || !Uri.TryCreate(rawUrl, UriKind.Absolute, out var parsed)
        )
        {
            return false;
        }

        var isHttp =
            string.Equals(parsed.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        if (!isHttp)
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    private static Dictionary<string, object?> BuildPayload(WorkflowActionExecutionContext context)
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["workflowId"] = context.WorkflowId,
            ["workflowNumber"] = context.WorkflowNumber,
            ["workflowName"] = context.WorkflowName,
            ["stateLabel"] = context.StateLabel,
            ["activityKey"] = context.ActivityKey,
            ["actionKey"] = context.ActionKey,
            ["attributes"] = context.WorkflowAttributes,
        };
    }

    private static bool AllowNonSuccess(IReadOnlyDictionary<string, string?> cfg)
    {
        return cfg.TryGetValue("IgnoreHttpErrors", out var flag)
            && string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
    }
}
