using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.SharedKernel.Results;

namespace Ruth.Providers.Checkr;

/// <summary>Modular Checkr REST + HMAC-SHA256 webhook background check provider.</summary>
public sealed class CheckrBackgroundCheckProvider(
    HttpClient httpClient,
    IOptions<CheckrOptions> options
) : IBackgroundCheckProvider
{
    /// <summary>Canonical lowercase provider key.</summary>
    public const string Key = "checkr";

    /// <summary>Standard Checkr HMAC-SHA256 webhook signature header.</summary>
    public const string SignatureHeader = "X-Checkr-Signature";

    private readonly CheckrOptions _options = options.Value;

    /// <inheritdoc />
    public string ProviderKey => Key;

    /// <inheritdoc />
    public string DisplayName => "Checkr";

    /// <inheritdoc />
    public string WebhookSignatureHeaderName => SignatureHeader;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BackgroundCheckPackageResponse>>> GetPackagesAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            IReadOnlyList<BackgroundCheckPackageResponse> defaults =
            [
                new BackgroundCheckPackageResponse(
                    Key,
                    _options.DefaultPackage,
                    "Checkr Standard Criminal",
                    "Default Checkr screening package (configure BackgroundChecks:Checkr:ApiKey for live sync)."
                ),
            ];
            return Result.Success(defaults);
        }

        using var request = CreateAuthorizedRequest(HttpMethod.Get, "v1/packages");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return DomainError.Failure(
                "Checkr.PackagesFailed",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Checkr packages request failed with HTTP {(int)response.StatusCode}."
                )
            );
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result.Success<IReadOnlyList<BackgroundCheckPackageResponse>>(ParsePackages(json));
    }

    /// <inheritdoc />
    public async Task<Result<BackgroundCheckSubmissionResponse>> SubmitRequestAsync(
        BackgroundCheckSubmissionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return DomainError.Failure(
                "Checkr.NotConfigured",
                "Checkr API key is not configured under BackgroundChecks:Checkr:ApiKey."
            );
        }

        var candidateResult = await CreateCandidateAsync(request, cancellationToken);
        if (candidateResult.IsFailure)
        {
            return Result.Failure<BackgroundCheckSubmissionResponse>(candidateResult.Errors);
        }

        var packageSlug = string.IsNullOrWhiteSpace(request.PackageSlug)
            ? _options.DefaultPackage
            : request.PackageSlug;

        using var inviteRequest = CreateAuthorizedRequest(HttpMethod.Post, "v1/invitations");
        inviteRequest.Content = JsonContent.Create(
            new { candidate_id = candidateResult.Value, package = packageSlug }
        );
        using var inviteResponse = await httpClient.SendAsync(inviteRequest, cancellationToken);
        if (!inviteResponse.IsSuccessStatusCode)
        {
            return DomainError.Failure(
                "Checkr.InvitationFailed",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Checkr invitation failed with HTTP {(int)inviteResponse.StatusCode}."
                )
            );
        }

        var inviteBody = await inviteResponse.Content.ReadAsStringAsync(cancellationToken);
        using var inviteDoc = JsonDocument.Parse(inviteBody);
        var invitationUrl =
            inviteDoc.RootElement.TryGetProperty("invitation_url", out var urlProp)
            && urlProp.ValueKind == JsonValueKind.String
                ? urlProp.GetString()
                : null;

        return Result.Success(
            new BackgroundCheckSubmissionResponse(
                candidateResult.Value,
                BackgroundCheckStatus.InvitationSent,
                "invitation.created",
                invitationUrl
            )
        );
    }

    /// <inheritdoc />
    public bool VerifyWebhookSignature(string rawPayload, string? signatureHeader)
    {
        return CheckrWebhookSignatureVerifier.Verify(
            rawPayload,
            signatureHeader,
            _options.EffectiveWebhookSecret
        );
    }

    /// <inheritdoc />
    public Result<BackgroundCheckWebhookEvent> ParseWebhookEvent(string rawPayload)
    {
        return CheckrWebhookParser.Parse(rawPayload);
    }

    private async Task<Result<string>> CreateCandidateAsync(
        BackgroundCheckSubmissionRequest request,
        CancellationToken cancellationToken
    )
    {
        using var candidateRequest = CreateAuthorizedRequest(HttpMethod.Post, "v1/candidates");
        candidateRequest.Content = JsonContent.Create(
            new
            {
                first_name = request.FirstName,
                middle_name = request.MiddleName,
                no_middle_name = string.IsNullOrWhiteSpace(request.MiddleName),
                last_name = request.LastName,
                email = request.Email,
            }
        );

        using var response = await httpClient.SendAsync(candidateRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return DomainError.Failure(
                "Checkr.CandidateFailed",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Checkr candidate creation failed with HTTP {(int)response.StatusCode}."
                )
            );
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(body);
        var id =
            doc.RootElement.TryGetProperty("id", out var idProp)
            && idProp.ValueKind == JsonValueKind.String
                ? idProp.GetString()
                : null;

        return string.IsNullOrWhiteSpace(id)
            ? DomainError.Failure("Checkr.CandidateMissingId", "Checkr returned no candidate ID.")
            : Result.Success(id);
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string relativePath)
    {
        var request = new HttpRequestMessage(method, new Uri(_options.BaseUrl, relativePath));
        var basicToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.ApiKey}:"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);
        return request;
    }

    private static List<BackgroundCheckPackageResponse> ParsePackages(string json)
    {
        var list = new List<BackgroundCheckPackageResponse>();
        using var doc = JsonDocument.Parse(json);
        if (
            !doc.RootElement.TryGetProperty("data", out var data)
            || data.ValueKind != JsonValueKind.Array
        )
        {
            return list;
        }

        foreach (var item in data.EnumerateArray())
        {
            var slug = item.TryGetProperty("slug", out var s) ? s.GetString() : null;
            var name = item.TryGetProperty("name", out var n) ? n.GetString() : slug;
            if (!string.IsNullOrWhiteSpace(slug))
            {
                list.Add(
                    new BackgroundCheckPackageResponse(Key, slug, name ?? slug, Description: null)
                );
            }
        }

        return list;
    }
}
