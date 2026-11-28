using System.Security.Claims;
using Ruth.Modules.Tenancy.Contracts.Hosts;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Host.Middleware;

/// <summary>
/// Write-once immutable tenant and host-plane binding for a single Blazor Server circuit.
/// </summary>
public sealed class TenantCircuitState
{
    /// <summary>Gets whether the circuit handshake binding has been sealed.</summary>
    public bool IsBound { get; private set; }

    /// <summary>Gets the immutable host classification captured at circuit handshake.</summary>
    public ResolvedHostKind HostKind { get; private set; } = ResolvedHostKind.Unknown;

    /// <summary>Gets the immutable tenant execution state captured at circuit handshake.</summary>
    public TenantExecutionState ExecutionState { get; private set; } =
        TenantExecutionState.Unresolved;

    /// <summary>Gets the last validated principal associated with the circuit.</summary>
    public ClaimsPrincipal? Principal { get; private set; }

    /// <summary>
    /// Binds the circuit once to its handshake host classification and tenant execution state,
    /// throwing <see cref="CrossTenantAccessException" /> if any subsequent call attempts to change plane or tenant.
    /// </summary>
    public void BindOrVerify(ResolvedHostKind hostKind, TenantExecutionState executionState)
    {
        ValidateHandshakePlane(hostKind, executionState);

        if (!IsBound)
        {
            HostKind = hostKind;
            ExecutionState = executionState;
            IsBound = true;
            return;
        }

        if (
            HostKind != hostKind
            || ExecutionState.TenantId != executionState.TenantId
            || ExecutionState.IsPlatformOperatorHost != executionState.IsPlatformOperatorHost
        )
        {
            throw new CrossTenantAccessException(
                "Blazor circuit tenant binding is immutable and cannot change after handshake."
            );
        }
    }

    /// <summary>
    /// Verifies that an inbound or reconnected ambient state does not conflict with the circuit's sealed tenant binding.
    /// </summary>
    public void EnsureCompatibleAmbientState(TenantExecutionState ambientState)
    {
        if (!IsBound)
        {
            throw new TenantContextRequiredException(
                "Blazor circuit cannot execute inbound activity before tenant handshake binding completes."
            );
        }

        if (ambientState == TenantExecutionState.Unresolved)
        {
            return;
        }

        if (
            ambientState.TenantId != ExecutionState.TenantId
            || ambientState.IsPlatformOperatorHost != ExecutionState.IsPlatformOperatorHost
        )
        {
            throw new CrossTenantAccessException(
                "Inbound Blazor circuit activity attempted to execute under a mismatched tenant context."
            );
        }
    }

    /// <summary>
    /// Validates <paramref name="principal" /> against the circuit's sealed host plane and tenant identifier.
    /// </summary>
    public void ValidateAndCapturePrincipal(ClaimsPrincipal? principal)
    {
        if (!IsBound)
        {
            throw new TenantContextRequiredException(
                "Blazor circuit must be bound before validating a security principal."
            );
        }

        ValidatePrincipalAgainstExecutionState(principal, HostKind, ExecutionState);
        Principal = principal;
    }

    internal static void ValidatePrincipalAgainstExecutionState(
        ClaimsPrincipal? principal,
        ResolvedHostKind hostKind,
        TenantExecutionState executionState
    )
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var isOperatorPrincipal = principal.HasClaim("platform_operator", "true");
        if (executionState.IsPlatformOperatorHost || hostKind == ResolvedHostKind.PlatformOperator)
        {
            if (!isOperatorPrincipal)
            {
                throw new CrossTenantAccessException(
                    "Tenant user credentials cannot execute inside a Platform Operator circuit."
                );
            }

            return;
        }

        if (isOperatorPrincipal)
        {
            throw new CrossTenantAccessException(
                "Platform Operator credentials cannot execute inside a tenant or signup circuit."
            );
        }

        if (hostKind == ResolvedHostKind.PlatformSignup)
        {
            if (principal.FindFirst("tid") is not null)
            {
                throw new CrossTenantAccessException(
                    "Tenant user credentials cannot execute inside the Platform Signup plane."
                );
            }

            return;
        }

        var tidClaim = principal.FindFirst("tid")?.Value;
        if (
            !Guid.TryParse(tidClaim, out var claimGuid)
            || claimGuid != executionState.TenantId.Value
        )
        {
            throw new CrossTenantAccessException(
                "Authenticated circuit principal tenant claim is missing or does not match the bound workspace."
            );
        }
    }

    private static void ValidateHandshakePlane(
        ResolvedHostKind hostKind,
        TenantExecutionState executionState
    )
    {
        switch (hostKind)
        {
            case ResolvedHostKind.TenantWorkspace:
                if (executionState.TenantId.IsEmpty || executionState.IsPlatformOperatorHost)
                {
                    throw new TenantContextRequiredException(
                        "Tenant workspace Blazor circuit requires a resolved non-empty TenantId."
                    );
                }

                break;
            case ResolvedHostKind.PlatformOperator:
                if (!executionState.IsPlatformOperatorHost || !executionState.TenantId.IsEmpty)
                {
                    throw new CrossTenantAccessException(
                        "Platform Operator Blazor circuit requires dedicated operator execution scope."
                    );
                }

                break;
            case ResolvedHostKind.PlatformSignup:
                if (!executionState.TenantId.IsEmpty || executionState.IsPlatformOperatorHost)
                {
                    throw new CrossTenantAccessException(
                        "Platform Signup Blazor circuit cannot bind to a tenant or operator execution scope."
                    );
                }

                break;
            case ResolvedHostKind.Unknown:
            default:
                throw new TenantContextRequiredException(
                    "Blazor circuit cannot open on an unknown or unverified host."
                );
        }
    }
}
