using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Contracts.Operators;

/// <summary>Authenticates an internal SaaS Platform Operator strictly on the dedicated operator host.</summary>
/// <param name="Email">The operator email address.</param>
/// <param name="Password">The operator password.</param>
public sealed record AuthenticatePlatformOperatorCommand(string Email, string Password)
    : ICommand<PlatformOperatorAuthResponse>,
        IPlatformControlPlaneRequest;
