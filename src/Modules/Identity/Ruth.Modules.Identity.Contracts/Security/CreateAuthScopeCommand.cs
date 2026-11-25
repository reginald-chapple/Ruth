using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Creates a new OAuth/OIDC scope.</summary>
public sealed record CreateAuthScopeCommand(string Name, string PublicName)
    : ICommand<AuthScopeResponse>;
