using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Identity.Infrastructure.Security;

internal static class JwtBearerSecurityEvents
{
    public const string PinnedAlgorithm = "HS256";

    private static readonly string[] _forbiddenDynamicKeyHeaders = ["jku", "x5u", "jwk", "x5c"];

    public static JwtBearerEvents Create()
    {
        return new JwtBearerEvents
        {
            OnMessageReceived = InspectRawBearerHeaderAsync,
            OnTokenValidated = ValidateJwtHeaderAndTenantBindingAsync,
        };
    }

    private static Task InspectRawBearerHeaderAsync(MessageReceivedContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var token = authHeader["Bearer ".Length..].Trim();
        var dotIdx = token.IndexOf('.', StringComparison.Ordinal);
        if (dotIdx <= 0)
        {
            context.Fail("Malformed JWT structure.");
            return Task.CompletedTask;
        }

        if (HasForbiddenJwtHeaderParameters(token[..dotIdx]))
        {
            context.Fail("JWT header contains forbidden algorithm or key-injection parameters.");
        }

        return Task.CompletedTask;
    }

    private static bool HasForbiddenJwtHeaderParameters(string encodedHeader)
    {
        try
        {
            var padded = encodedHeader.Replace('-', '+').Replace('_', '/');
            padded = (padded.Length % 4) switch
            {
                2 => padded + "==",
                3 => padded + "=",
                _ => padded,
            };
            using var doc = JsonDocument.Parse(Convert.FromBase64String(padded));
            var root = doc.RootElement;

            if (
                !root.TryGetProperty("alg", out var algProp)
                || !string.Equals(algProp.GetString(), PinnedAlgorithm, StringComparison.Ordinal)
            )
            {
                return true;
            }

            foreach (var forbidden in _forbiddenDynamicKeyHeaders)
            {
                if (root.TryGetProperty(forbidden, out _))
                {
                    return true;
                }
            }

            return root.TryGetProperty("kid", out var kidProp)
                && kidProp.GetString() is { } kid
                && (
                    kid.Contains("..", StringComparison.Ordinal)
                    || kid.Contains('/', StringComparison.Ordinal)
                    || kid.Contains('\\', StringComparison.Ordinal)
                );
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return true;
        }
    }

    private static Task ValidateJwtHeaderAndTenantBindingAsync(TokenValidatedContext context)
    {
        if (
            context.SecurityToken is not JsonWebToken jwt
            || !string.Equals(jwt.Alg, PinnedAlgorithm, StringComparison.Ordinal)
        )
        {
            context.Fail("JWT signing algorithm is not pinned to HS256.");
            return Task.CompletedTask;
        }

        var state = AmbientTenantContext.CurrentState;
        var isOperatorPrincipal =
            context.Principal?.HasClaim(RuthClaimTypes.PlatformOperator, "true") == true;
        var tokenTid = context.Principal?.FindFirst(RuthClaimTypes.TenantId)?.Value;

        if (state.IsPlatformOperatorHost)
        {
            if (!isOperatorPrincipal || !string.IsNullOrEmpty(tokenTid))
            {
                context.Fail("Tenant credentials are not valid on the Platform Operator host.");
            }

            return Task.CompletedTask;
        }

        if (isOperatorPrincipal)
        {
            context.Fail("Platform Operator credentials cannot authenticate on tenant hosts.");
            return Task.CompletedTask;
        }

        if (state.TenantId.IsEmpty)
        {
            context.Fail("JWT access tokens require an active tenant workspace host.");
            return Task.CompletedTask;
        }

        if (!Guid.TryParse(tokenTid, out var parsed) || parsed != state.TenantId.Value)
        {
            context.Fail("Access token tenant claim does not match active workspace.");
        }

        return Task.CompletedTask;
    }
}
